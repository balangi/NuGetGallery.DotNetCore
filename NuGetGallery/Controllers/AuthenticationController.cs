// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NuGet.Services.Entities;
using NuGet.Services.Messaging.Email;
using NuGetGallery.Authentication;
using NuGetGallery.Authentication.Providers;
using NuGetGallery.Authentication.Providers.AzureActiveDirectoryV2;
using NuGetGallery.Authentication.Providers.MicrosoftAccount;
using NuGetGallery.Infrastructure.Authentication;
using NuGetGallery.Infrastructure.Mail.Messages;
using NuGetGallery.Security;
using NuGetAuthService = NuGetGallery.Authentication.AuthenticationService;

namespace NuGetGallery;

public static class AuthenticationFailureErrors
{
    public const string ACCESSS_DENIED = "access_denied";
    public const string CONSENT_REQUIRED = "consent_required";
}

public partial class AuthenticationController : AppController
{
    private readonly NuGetAuthService _authService;
    private readonly IUserService _userService;
    private readonly IMessageService _messageService;
    private readonly ICredentialBuilder _credentialBuilder;
    private readonly IContentObjectService _contentObjectService;
    private readonly IMessageServiceConfiguration _messageServiceConfiguration;
    private readonly IFeatureFlagService _featureFlagService;
    private const string EMAIL_FORMAT_PADDING = "**********";

    private static readonly string[] ExternalAuthenticationPriority = new string[] {
        Authenticator.GetName(typeof(AzureActiveDirectoryV2Authenticator)),
        Authenticator.GetName(typeof(MicrosoftAccountAuthenticator))
    };

    public AuthenticationController(
        NuGetAuthService authService,
        IUserService userService,
        IMessageService messageService,
        ICredentialBuilder credentialBuilder,
        IContentObjectService contentObjectService,
        IMessageServiceConfiguration messageServiceConfiguration,
        IFeatureFlagService featureFlagService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _credentialBuilder = credentialBuilder ?? throw new ArgumentNullException(nameof(credentialBuilder));
        _contentObjectService = contentObjectService ?? throw new ArgumentNullException(nameof(contentObjectService));
        _messageServiceConfiguration = messageServiceConfiguration ?? throw new ArgumentNullException(nameof(messageServiceConfiguration));
        _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
    }

    [HttpGet]
    public virtual IActionResult LogOn(string returnUrl)
    {
        ViewData[GalleryConstants.ReturnUrlViewDataKey] = returnUrl;

        if (TempData.ContainsKey(GalleryConstants.ReturnUrlMessageViewDataKey))
        {
            ViewData[GalleryConstants.ReturnUrlMessageViewDataKey] = TempData[GalleryConstants.ReturnUrlMessageViewDataKey];
        }

        if (User.Identity.IsAuthenticated)
        {
            return LoggedInRedirect(returnUrl);
        }

        return SignInView(new LogOnViewModel());
    }

    [HttpGet]
    public virtual IActionResult LogOnNuGetAccount(string returnUrl)
    {
        ViewData[GalleryConstants.ReturnUrlViewDataKey] = returnUrl;

        if (User.Identity.IsAuthenticated)
        {
            return LoggedInRedirect(returnUrl);
        }

        return SignInNuGetAccountView(new LogOnViewModel());
    }

    [HttpGet]
    public virtual IActionResult SignUp(string returnUrl)
    {
        ViewData[GalleryConstants.ReturnUrlViewDataKey] = returnUrl;

        if (User.Identity.IsAuthenticated)
        {
            return LoggedInRedirect(returnUrl);
        }
        else if (_featureFlagService.IsNewAccount2FAEnforcementEnabled())
        {
            return Redirect(Url.Action("LogOn", "Authentication", new { returnUrl = (string)null }));
        }

        return RegisterView(new LogOnViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> SignIn(LogOnViewModel model, string returnUrl, bool linkingAccount)
    {
        ViewData[GalleryConstants.ReturnUrlViewDataKey] = returnUrl;

        if (User.Identity.IsAuthenticated)
        {
            return LoggedInRedirect(returnUrl);
        }

        if (!ModelState.IsValid)
        {
            return SignInOrExternalLinkView(model, linkingAccount);
        }

        var authenticationResult = await _authService.Authenticate(model.SignIn.UserNameOrEmail, model.SignIn.Password);

        if (authenticationResult.Result != PasswordAuthenticationResult.AuthenticationResult.Success)
        {
            string modelErrorMessage = string.Empty;

            if (authenticationResult.Result == PasswordAuthenticationResult.AuthenticationResult.PasswordLoginUnsupported)
            {
                modelErrorMessage = Strings.NuGetAccountPasswordLoginUnsupported;
            }
            else if (authenticationResult.Result == PasswordAuthenticationResult.AuthenticationResult.BadCredentials)
            {
                modelErrorMessage = Strings.UsernameAndPasswordNotFound;
            }
            else if (authenticationResult.Result == PasswordAuthenticationResult.AuthenticationResult.AccountLocked)
            {
                string timeRemaining =
                    authenticationResult.LockTimeRemainingMinutes == 1
                        ? Strings.AMinute
                        : string.Format(CultureInfo.CurrentCulture, Strings.Minutes,
                            authenticationResult.LockTimeRemainingMinutes);

                modelErrorMessage = string.Format(CultureInfo.CurrentCulture, Strings.UserAccountLocked, timeRemaining);
            }

            return SignInFailure(model, linkingAccount, modelErrorMessage);
        }

        var authenticatedUser = authenticationResult.AuthenticatedUser;
        bool usedMultiFactorAuthentication = false;
        if (linkingAccount)
        {
            if (authenticatedUser.User.Credentials.Any(c => c.IsExternal()) && !authenticatedUser.User.IsAdministrator)
            {
                var message = string.Format(
                       CultureInfo.CurrentCulture,
                       Strings.AccountIsLinkedToAnotherExternalAccount,
                       authenticatedUser.User.EmailAddress);
                return SignInFailure(model, linkingAccount, message);
            }

            var loginUserDetails = await AssociateCredential(authenticatedUser);
            authenticatedUser = loginUserDetails?.AuthenticatedUser;
            if (authenticatedUser == null)
            {
                return AuthenticationFailureOrExternalLinkExpired();
            }

            usedMultiFactorAuthentication = loginUserDetails.UsedMultiFactorAuthentication;
        }

        IActionResult challenge;
        if (ShouldChallengeEnforcedProvider(
            NuGetContext.Config.Current.EnforcedAuthProviderForAdmin, authenticatedUser, returnUrl, out challenge))
        {
            return challenge;
        }

        if (!SiteAdminHasValidTenant(authenticatedUser))
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, Strings.SiteAdminNotLoggedInWithRequiredTenant, NuGetContext.Config.Current.EnforcedTenantIdForAdmin);
            return SignInFailure(model, linkingAccount, errorMessage);
        }

        // ایجاد نشست با استفاده از HttpContext
        await _authService.CreateSessionAsync(HttpContext, authenticatedUser, usedMultiFactorAuthentication);

        TempData["ShowPasswordDeprecationWarning"] = true;
        return SafeRedirect(returnUrl);
    }

    private IActionResult SignInFailure(LogOnViewModel model, bool linkingAccount, string modelErrorMessage)
    {
        ModelState.AddModelError("SignIn", modelErrorMessage);
        return SignInOrExternalLinkView(model, linkingAccount);
    }

    internal bool ShouldChallengeEnforcedProvider(string enforcedProviders, AuthenticatedUser authenticatedUser, string returnUrl, out IActionResult challenge)
    {
        if (!string.IsNullOrEmpty(enforcedProviders)
            && authenticatedUser.CredentialUsed.Type != null
            && authenticatedUser.User.IsAdministrator)
        {
            var providers = enforcedProviders.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (!providers.Any(p => string.Equals(p, authenticatedUser.CredentialUsed.Type, StringComparison.OrdinalIgnoreCase))
                && !providers.Any(p => string.Equals(CredentialTypes.External.Prefix + p, authenticatedUser.CredentialUsed.Type, StringComparison.OrdinalIgnoreCase)))
            {
                challenge = _authService.Challenge(
                    providers.First(),
                    Url.Action("LinkExternalAccount", "Authentication", new { returnUrl }));

                return true;
            }
        }

        challenge = null;
        return false;
    }

    internal bool SiteAdminHasValidTenant(AuthenticatedUser authenticatedUser)
    {
        if (!string.IsNullOrEmpty(NuGetContext.Config.Current.EnforcedTenantIdForAdmin)
            && authenticatedUser.User.IsAdministrator)
        {
            return string.Equals(NuGetContext.Config.Current.EnforcedTenantIdForAdmin, authenticatedUser.CredentialUsed.TenantId, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    [HttpGet]
    public virtual IActionResult RegisterLegacy(string returnUrl)
    {
        if (_featureFlagService.IsNewAccount2FAEnforcementEnabled())
        {
            return Redirect(Url.Action("LogOn", "Authentication"));
        }

        return Redirect(Url.Action("LogOnNuGetAccount", "Authentication", new { returnUrl }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> Register(LogOnViewModel model, string returnUrl, bool linkingAccount)
    {
        ViewData[GalleryConstants.ReturnUrlViewDataKey] = returnUrl;

        if (User.Identity.IsAuthenticated)
        {
            TempData["Message"] = Strings.AlreadyLoggedIn;
            return SafeRedirect(returnUrl);
        }

        if (linkingAccount)
        {
            ModelState.Remove("Register.Password");
        }

        if (!ModelState.IsValid)
        {
            return RegisterOrExternalLinkView(model, linkingAccount);
        }

        AuthenticatedUser user;
        var usedMultiFactorAuthentication = false;
        try
        {
            if (linkingAccount)
            {
                var result = await _authService.ReadExternalLoginCredential(HttpContext);
                if (result.ExternalIdentity == null)
                {
                    return AuthenticationFailureOrExternalLinkExpired();
                }

                usedMultiFactorAuthentication = result.LoginDetails?.WasMultiFactorAuthenticated ?? false;
                var enableMultiFactorAuthentication = _featureFlagService.IsNewAccount2FAEnforcementEnabled() ? true : usedMultiFactorAuthentication;
                user = await _authService.Register(
                    model.Register.Username,
                    model.Register.EmailAddress,
                    result.Credential,
                    enableMultiFactorAuthentication: enableMultiFactorAuthentication);
            }
            else if (_featureFlagService.IsNewAccount2FAEnforcementEnabled())
            {
                return Redirect(Url.Action("LogOn", "Authentication"));
            }
            else
            {
                user = await _authService.Register(
                    model.Register.Username,
                    model.Register.EmailAddress,
                    _credentialBuilder.CreatePasswordCredential(model.Register.Password));
            }
        }
        catch (EntityException ex)
        {
            ModelState.AddModelError("Register", ex.Message);
            return RegisterOrExternalLinkView(model, linkingAccount);
        }

        if (NuGetContext.Config.Current.ConfirmEmailAddresses && !string.IsNullOrEmpty(user.User.UnconfirmedEmailAddress))
        {
            var message = new NewAccountMessage(
                _messageServiceConfiguration,
                user.User,
                Url.Action("ConfirmEmail", "Users", new { username = user.User.Username, token = user.User.EmailConfirmationToken }, protocol: Request.Scheme));

            await _messageService.SendMessageAsync(message);
        }

        IActionResult challenge;
        if (ShouldChallengeEnforcedProvider(
            NuGetContext.Config.Current.EnforcedAuthProviderForAdmin, user, returnUrl, out challenge))
        {
            return challenge;
        }

        if (!SiteAdminHasValidTenant(user))
        {
            ModelState.AddModelError("Register", string.Format(Strings.SiteAdminNotLoggedInWithRequiredTenant, NuGetContext.Config.Current.EnforcedTenantIdForAdmin));
            return RegisterOrExternalLinkView(model, linkingAccount);
        }

        await _authService.CreateSessionAsync(HttpContext, user, usedMultiFactorAuthentication);
        return RedirectFromRegister(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual IActionResult LogOff(string returnUrl)
    {
        HttpContext.SignOutAsync();

        if (!string.IsNullOrEmpty(returnUrl)
            && (returnUrl.Contains("account", StringComparison.OrdinalIgnoreCase) || returnUrl.Contains("Admin", StringComparison.OrdinalIgnoreCase)))
        {
            returnUrl = null;
        }

        return SafeRedirect(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<JsonResult> SignInAssistance(string username, string providedEmailAddress)
    {
        try
        {
            var user = _userService.FindByUsername(username);
            if (user == null)
            {
                throw new ArgumentException(Strings.UserNotFound);
            }

            var email = user.EmailAddress ?? user.UnconfirmedEmailAddress;
            if (string.IsNullOrWhiteSpace(providedEmailAddress))
            {
                var formattedEmail = FormatEmailAddressForAssistance(email);
                return Json(new { success = true, EmailAddress = formattedEmail });
            }
            else
            {
                if (!email.Equals(providedEmailAddress, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(Strings.SigninAssistance_EmailMismatched);
                }
                else
                {
                    var message = new SigninAssistanceMessage(
                        _messageServiceConfiguration,
                        new MailAddress(email, user.Username),
                        user.Credentials.Where(cred => cred.IsExternal()));

                    await _messageService.SendMessageAsync(message);
                    return Json(new { success = true });
                }
            }
        }
        catch (ArgumentException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [ActionName("Authenticate")]
    [HttpGet]
    public virtual IActionResult AuthenticateGet(string returnUrl, string provider)
    {
        if (provider == null || !_authService.Authenticators.ContainsKey(provider))
        {
            TempData["ErrorMessage"] = ServicesStrings.AuthenticationProviderNotFound;
            return SafeRedirect(returnUrl);
        }

        return AuthenticateAndLinkExternal(returnUrl, provider);
    }

    [ActionName("Authenticate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual IActionResult AuthenticatePost(string returnUrl, string provider)
    {
        return AuthenticateAndLinkExternal(returnUrl, provider);
    }

    [ActionName("AuthenticateExternal")]
    [HttpGet]
    public virtual IActionResult AuthenticateExternal(string returnUrl)
    {
        var user = GetCurrentUser();
        var userOrganizationsWithTenantPolicy = user?
            .Organizations?
            .Where(o => o
                .Organization
                .SecurityPolicies
                .Any(policy => policy.Name == nameof(RequireOrganizationTenantPolicy)));

        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        if (userOrganizationsWithTenantPolicy != null && userOrganizationsWithTenantPolicy.Any())
        {
            var aadCredential = user?.Credentials.GetAzureActiveDirectoryCredential();
            if (aadCredential != null)
            {
                var orgList = string.Join(", ",
                    userOrganizationsWithTenantPolicy.Select(member => member.Organization.Username));

                TempData["WarningMessage"] = string.Format(Strings.ChangeCredential_NotAllowed, orgList);
                return Redirect(returnUrl);
            }
        }

        var externalAuthProvider = GetExternalProvider();
        if (externalAuthProvider == null)
        {
            TempData["Message"] = Strings.ChangeCredential_ProviderNotFound;
            return Redirect(returnUrl);
        }

        return ChallengeAuthentication(Url.Action("LinkOrChangeExternalCredential", "Authentication", new { returnUrl }), externalAuthProvider);
    }

    [NonAction]
    public IActionResult AuthenticateAndLinkExternal(string returnUrl, string provider)
    {
        TempData["ShowPasswordDeprecationWarning"] = true;
        return ChallengeAuthentication(Url.Action("LinkExternalAccount", "Authentication", new { returnUrl }), provider);
    }

    [NonAction]
    public IActionResult ChallengeAuthentication(string returnUrl, string provider, AuthenticationPolicy policy = null)
    {
        try
        {
            return _authService.Challenge(provider, returnUrl, policy);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public virtual async Task<IActionResult> LinkOrChangeExternalCredential(string returnUrl)
    {
        var user = GetCurrentUser();
        if (user.IsLocked)
        {
            TempData["ErrorMessage"] = ServicesStrings.UserAccountIsLocked;
            return SafeRedirect(returnUrl);
        }

        var result = await _authService.ReadExternalLoginCredential(HttpContext);
        if (result?.Credential == null)
        {
            TempData["ErrorMessage"] = Strings.ExternalAccountLinkExpired;
            return SafeRedirect(returnUrl);
        }

        if (_featureFlagService.IsNewAccount2FAEnforcementEnabled() && !result.UserInfo.UsedMultiFactorAuthentication)
        {
            return ChallengeAuthentication(
                Url.Action("LinkOrChangeExternalCredential", "Authentication", new { returnUrl }),
                result.Authenticator.Name,
                new AuthenticationPolicy() { Email = result.LoginDetails.EmailUsed, EnforceMultiFactorAuthentication = true });
        }

        var newCredential = result.Credential;
        if (await _authService.TryReplaceCredential(user, newCredential))
        {
            await RemovePasswordCredential(user);

            var authenticatedUser = await _authService.Authenticate(newCredential);
            var usedMultiFactorAuthentication = result.LoginDetails?.WasMultiFactorAuthenticated ?? false;
            await _authService.CreateSessionAsync(HttpContext, authenticatedUser, usedMultiFactorAuthentication);

            if (result?.LoginDetails != null
                && usedMultiFactorAuthentication
                && !user.EnableMultiFactorAuthentication
                && CredentialTypes.IsExternal(result.Credential))
            {
                await _userService.ChangeMultiFactorAuthentication(user, enableMultiFactor: true, referrer: "Authentication");
                // جایگزینی AddClaim روی HttpContext
                if (HttpContext.User.Identity is ClaimsIdentity claimsIdentity)
                {
                    claimsIdentity.AddClaim(new Claim(NuGetClaims.EnabledMultiFactorAuthentication, "true"));
                }
            }

            var newEmailAddress = GetEmailAddressFromExternalLoginResult(result, out string errorReason);
            if (!string.IsNullOrEmpty(errorReason))
            {
                TempData["ErrorMessage"] = errorReason;
                return SafeRedirect(returnUrl);
            }

            var linkingDifferentEmailAddress = !newEmailAddress.Equals(user.EmailAddress, StringComparison.OrdinalIgnoreCase);
            TempData["Message"] = linkingDifferentEmailAddress
                ? string.Format(Strings.ChangeCredential_SuccessDifferentEmail, newEmailAddress, user.EmailAddress)
                : string.Format(Strings.ChangeCredential_Success, newEmailAddress);
        }
        else
        {
            TempData["RawErrorMessage"] = string.Format(Strings.ChangeCredential_Failed,
                WebUtility.HtmlEncode(newCredential.Identity),
                UriExtensions.GetExternalUrlAnchorTag("FAQs page", GalleryConstants.FAQLinks.MSALinkedToAnotherAccount));
        }

        return SafeRedirect(returnUrl);
    }

    [SuppressMessage("Security", "CA3147:Mark Verb Handlers With Validate Antiforgery Token", Justification = "Disable antiforgery token validation due to actions handle GET/POST requests")]
    public virtual async Task<IActionResult> LinkExternalAccount(string returnUrl, string error = null, string errorDescription = null)
    {
        var result = await _authService.AuthenticateExternalLogin(HttpContext);
        if (result.ExternalIdentity == null)
        {
            string errorMessage = GetAuthenticationFailureMessage(error, errorDescription);
            return AuthenticationFailureOrExternalLinkExpired(errorMessage);
        }

        if (result.Authentication != null)
        {
            IActionResult challenge;
            if (ShouldChallengeEnforcedProvider(
                NuGetContext.Config.Current.EnforcedAuthProviderForAdmin, result.Authentication, returnUrl, out challenge))
            {
                return challenge;
            }

            if (!SiteAdminHasValidTenant(result.Authentication))
            {
                string errorMessage = string.Format(Strings.SiteAdminNotLoggedInWithRequiredTenant, NuGetContext.Config.Current.EnforcedTenantIdForAdmin);
                return AuthenticationFailureOrExternalLinkExpired(errorMessage);
            }

            if (ShouldEnforceMultiFactorAuthentication(result))
            {
                return ChallengeAuthentication(
                    Url.Action("LinkExternalAccount", "Authentication", new { returnUrl }),
                    result.Authenticator.Name,
                    new AuthenticationPolicy() { Email = result.LoginDetails.EmailUsed, EnforceMultiFactorAuthentication = true });
            }

            if (NuGetContext.Config.Current.DeprecateNuGetPasswordLogins
                && _contentObjectService.LoginDiscontinuationConfiguration.IsPasswordLoginDiscontinuedForAll()
                && result.Authentication.CredentialUsed.IsExternal()
                && result.Authentication.User.HasPasswordCredential())
            {
                TempData["Message"] = string.Format(Strings.DiscontinuedLogin_PasswordRemoved, NuGetContext.Config.Current.Brand);
                await RemovePasswordCredential(result.Authentication.User);
            }

            await _authService.CreateSessionAsync(HttpContext,
                result.Authentication,
                wasMultiFactorAuthenticated: result?.LoginDetails?.WasMultiFactorAuthenticated ?? false);

            if (result?.LoginDetails != null
                && result.LoginDetails.WasMultiFactorAuthenticated
                && !result.Authentication.User.EnableMultiFactorAuthentication
                && CredentialTypes.IsExternal(result.Credential))
            {
                await _userService.ChangeMultiFactorAuthentication(result.Authentication.User, enableMultiFactor: true, referrer: "Authentication");
                if (HttpContext.User.Identity is ClaimsIdentity claimsIdentity)
                {
                    claimsIdentity.AddClaim(new Claim(NuGetClaims.EnabledMultiFactorAuthentication, "true"));
                }
                if (CredentialTypes.IsMicrosoftAccount(result.Credential.Type))
                {
                    TempData["Message"] = Strings.MultiFactorAuth_LoginUpdate;
                }
            }

            var currentUser = result.Authentication.User;
            if (!currentUser.EnableMultiFactorAuthentication
                && CredentialTypes.IsMicrosoftAccount(result.Credential.Type))
            {
                TempData[GalleryConstants.AskUserToEnable2FA] = true;
            }

            return SafeRedirect(returnUrl);
        }
        else if (_featureFlagService.IsNewAccount2FAEnforcementEnabled() && CredentialTypes.IsExternal(result.Credential) && !result.LoginDetails.WasMultiFactorAuthenticated)
        {
            return ChallengeAuthentication(
                Url.Action("LinkExternalAccount", "Authentication", new { returnUrl }),
                result.Authenticator.Name,
                new AuthenticationPolicy() { Email = result.LoginDetails.EmailUsed, EnforceMultiFactorAuthentication = true });
        }
        else
        {
            string name = null;
            string email = null;
            var authUI = result.Authenticator.GetUI();
            try
            {
                var userInfo = result.Authenticator.GetIdentityInformation(result.ExternalIdentity);
                name = userInfo.Name;
                email = userInfo.Email;
            }
            catch (Exception)
            {
                email = result.ExternalIdentity.GetClaimOrDefault(ClaimTypes.Email);
                name = result.ExternalIdentity.GetClaimOrDefault(ClaimTypes.Name);
            }

            User existingUser = null;
            if (!string.IsNullOrEmpty(email))
            {
                existingUser = _userService.FindByEmailAddress(email);
            }

            var foundExistingUser = existingUser != null;
            var existingUserLinkingError = AssociateExternalAccountViewModel.ExistingUserLinkingErrorType.None;

            if (foundExistingUser)
            {
                if (existingUser is Organization)
                {
                    existingUserLinkingError = AssociateExternalAccountViewModel.ExistingUserLinkingErrorType.AccountIsOrganization;
                }
                else if (existingUser.Credentials.Any(c => c.IsExternal()) && !existingUser.IsAdministrator)
                {
                    existingUserLinkingError = AssociateExternalAccountViewModel.ExistingUserLinkingErrorType.AccountIsAlreadyLinked;
                }
            }

            var external = new AssociateExternalAccountViewModel()
            {
                ProviderAccountNoun = authUI.AccountNoun,
                AccountName = name,
                FoundExistingUser = foundExistingUser,
                ExistingUserLinkingError = existingUserLinkingError
            };

            var model = new LogOnViewModel
            {
                External = external,
                SignIn = new SignInViewModel
                {
                    UserNameOrEmail = email
                },
                Register = new RegisterViewModel
                {
                    EmailAddress = email
                }
            };

            return LinkExternalView(model);
        }
    }

    internal bool ShouldEnforceMultiFactorAuthentication(AuthenticateExternalLoginResult result)
    {
        if (result?.Authenticator == null || result.Authentication == null)
        {
            return false;
        }

        return result.Authenticator.SupportsMultiFactorAuthentication()
            && result.Authentication.User.EnableMultiFactorAuthentication
            && !result.LoginDetails.WasMultiFactorAuthenticated
            && result.Authentication.CredentialUsed.IsExternal()
            && (_featureFlagService.IsNewAccount2FAEnforcementEnabled() || CredentialTypes.IsMicrosoftAccount(result.Authentication.CredentialUsed.Type));
    }

    private string FormatEmailAddressForAssistance(string email)
    {
        var emailParts = email.Split('@');
        if (emailParts.Length != 2)
        {
            throw new ArgumentException(@"Invalid email address. Please contact support for assistance.");
        }

        var idPart = emailParts[0];
        var domainPart = emailParts[1];
        return idPart[0]
          + EMAIL_FORMAT_PADDING
          + (idPart.Length > 1 ? idPart[idPart.Length - 1] + "" : "")
          + $"@{domainPart}";
    }

    private IActionResult RedirectFromRegister(string returnUrl)
    {
        if (returnUrl != Url.Action("Home", "Pages"))
        {
            TempData["Message"] = "Your account is now registered!";
            return SafeRedirect(returnUrl);
        }

        return RedirectToAction(actionName: "Thanks", controllerName: "Users");
    }

    private async Task<LoginUserDetails> AssociateCredential(AuthenticatedUser user)
    {
        var result = await _authService.ReadExternalLoginCredential(HttpContext);
        if (result.ExternalIdentity == null)
        {
            return null;
        }

        await _authService.AddCredential(user.User, result.Credential);
        await RemovePasswordCredential(user.User);

        var emailMessage = new CredentialAddedMessage(
            _messageServiceConfiguration,
            user.User,
            _authService.DescribeCredential(result.Credential).GetCredentialTypeInfo());
        await _messageService.SendMessageAsync(emailMessage);

        return new LoginUserDetails
        {
            AuthenticatedUser = new AuthenticatedUser(user.User, result.Credential),
            UsedMultiFactorAuthentication = result.LoginDetails?.WasMultiFactorAuthenticated ?? false
        };
    }

    private async Task RemovePasswordCredential(User user)
    {
        var passwordCredential = user.GetPasswordCredential();
        if (passwordCredential != null)
        {
            await _authService.RemoveCredential(user, passwordCredential);
        }
    }

    private List<AuthenticationProviderViewModel> GetProviders()
    {
        return (from p in _authService.Authenticators.Values
                where p.BaseConfig.Enabled
                let ui = p.GetUI()
                where ui != null && ui.ShowOnLoginPage
                select new AuthenticationProviderViewModel()
                {
                    ProviderName = p.Name,
                    UI = ui
                }).ToList();
    }

    private string GetExternalProvider()
    {
        var providers = GetProviders();
        return ExternalAuthenticationPriority
            .FirstOrDefault(authenticator => providers.Any(p => p.ProviderName.Equals(authenticator, StringComparison.OrdinalIgnoreCase)));
    }

    private string GetEmailAddressFromExternalLoginResult(AuthenticateExternalLoginResult result, out string errorReason)
    {
        try
        {
            var userInformation = result.Authenticator?.GetIdentityInformation(result.ExternalIdentity);
            errorReason = null;
            return userInformation.Email;
        }
        catch (ArgumentException ex)
        {
            errorReason = ex.Message;
            return null;
        }
    }

    private IActionResult AuthenticationFailureOrExternalLinkExpired(string errorMessage = null)
    {
        if (!string.IsNullOrEmpty(errorMessage) &&
            errorMessage.IndexOf("AADSTS65005", StringComparison.OrdinalIgnoreCase) > -1 &&
            errorMessage.IndexOf("unmanaged", StringComparison.OrdinalIgnoreCase) > -1)
        {
            TempData["RawErrorMessage"] = errorMessage + "<br/>" + string.Format(Strings.DirectUserToUnmanagedTenantFAQ,
                UriExtensions.GetExternalUrlAnchorTag("FAQs page", GalleryConstants.FAQLinks.AccountBelongsToUnmanagedTenant));
        }
        else
        {
            TempData["ErrorMessage"] = string.IsNullOrEmpty(errorMessage) ? Strings.ExternalAccountLinkExpired : errorMessage;
        }

        return Redirect(Url.Action("LogOn", "Authentication"));
    }

    private IActionResult SignInOrExternalLinkView(LogOnViewModel model, bool linkingAccount)
    {
        if (linkingAccount)
        {
            return LinkExternalView(model);
        }
        else
        {
            return SignInNuGetAccountView(model);
        }
    }

    private IActionResult RegisterOrExternalLinkView(LogOnViewModel model, bool linkingAccount)
    {
        if (linkingAccount)
        {
            return LinkExternalView(model);
        }
        else
        {
            return RegisterView(model);
        }
    }

    private IActionResult LoggedInRedirect(string returnUrl)
    {
        TempData["Message"] = Strings.AlreadyLoggedIn;
        return SafeRedirect(returnUrl);
    }

    private IActionResult SignInView(LogOnViewModel existingModel)
    {
        return AuthenticationView("SignIn", existingModel);
    }

    private IActionResult RegisterView(LogOnViewModel existingModel)
    {
        return AuthenticationView("Register", existingModel);
    }

    private IActionResult SignInNuGetAccountView(LogOnViewModel existingModel)
    {
        return AuthenticationView("SignInNuGetAccount", existingModel);
    }

    private class ClaimsPrincipalExtension
    {
        // کمکی برای افزودن کلایم در صورت نیاز
    }

    private IActionResult LinkExternalView(LogOnViewModel existingModel)
    {
        return AuthenticationView("LinkExternal", existingModel);
    }

    private IActionResult AuthenticationView(string viewName, LogOnViewModel existingModel)
    {
        existingModel.Providers = GetProviders();
        existingModel.SignIn = existingModel.SignIn ?? new SignInViewModel();
        existingModel.Register = existingModel.Register ?? new RegisterViewModel();
        existingModel.IsNuGetAccountPasswordLoginEnabled = _featureFlagService.IsNuGetAccountPasswordLoginEnabled();
        existingModel.IsEmailOnExceptionList = _contentObjectService.LoginDiscontinuationConfiguration.IsEmailInExceptionsList(existingModel.SignIn.UserNameOrEmail);
        return View(viewName, existingModel);
    }

    private string GetAuthenticationFailureMessage(string error, string errorDescription)
    {
        if (string.IsNullOrEmpty(error))
        {
            return Strings.AuthenticationFailure_UnkownError;
        }

        switch (error)
        {
            case AuthenticationFailureErrors.ACCESSS_DENIED:
            case AuthenticationFailureErrors.CONSENT_REQUIRED:
                return Strings.ExternalAccountLinkExpired;
            default:
                return string.IsNullOrEmpty(errorDescription)
                    ? error
                    : errorDescription;
        }
    }
}
