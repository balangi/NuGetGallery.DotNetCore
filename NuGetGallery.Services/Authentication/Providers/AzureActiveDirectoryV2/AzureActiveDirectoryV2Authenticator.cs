// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NuGetGallery.Configuration;
using System.Security.Claims;

namespace NuGetGallery.Authentication.Providers.AzureActiveDirectoryV2;

public class AzureActiveDirectoryV2Authenticator : Authenticator<AzureActiveDirectoryV2AuthenticatorConfiguration>
{
    public static class V2Claims
    {
        public const string EmailAddress = ClaimTypes.Email;
        public const string Identifier = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        public const string Issuer = "iss";
        public const string Name = "name";
        public const string PreferredUsername = "preferred_username";
        public const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";

        /// <summary>
        /// ACR is the Authentication Class Reference token, which is the claim that is returned by OpenId upon usage of multi-factor during authentication.
        /// More details: http://openid.net/specs/openid-connect-eap-acr-values-1_0.html
        /// </summary>
        public const string ACR = "http://schemas.microsoft.com/claims/authnclassreference";
    }

    public static class AuthenticationType
    {
        public const string MicrosoftAccount = "MicrosoftAccount";
        public const string AzureActiveDirectory = "AzureActiveDirectory";
    }

    public static readonly string DefaultAuthenticationType = "AzureActiveDirectoryV2";
    public static readonly string PersonalMSATenant = "9188040d-6c67-4c5b-b112-36a304b66dad";
    public static readonly string Authority = "https://login.microsoftonline.com/{0}/v2.0";

    private static string _callbackPath = "/users/account/authenticate/return";
    private static HashSet<string> _alternateSiteRootList;
    private const string SELECT_ACCOUNT = "select_account";

    private static class ErrorQueryKeys
    {
        public const string Error = "error";
        public const string ErrorDescription = "errorDescription";
    }

    /// <summary>
    /// The possible values returned by <see cref="V2Claims.ACR"/> claim, and also the possible token values to be sent
    /// for authentication to the common endpoint.
    /// </summary>
    public static class ACR_VALUES
    {
        public static readonly string DEFAULT = "urn:microsoft:policies:default";
        public static readonly string MFA = "urn:microsoft:policies:mfa";

        /// <summary>
        /// Combination of MFA and DEFAULT values sent as id_token to the authentication uses the user set policy
        /// for multi-factor authentication and returns the <see cref="V2Claims.ACR"/> token with the used policy.
        /// </summary>
        public static readonly string ANY = MFA + " " + DEFAULT;
    }

    protected override void AttachToApp(IGalleryConfigurationService config, AuthenticationBuilder authBuilder)
    {
        // Fetch site root from configuration
        var siteRoot = config.Current.SiteRoot.TrimEnd('/') + "/";

        // We *always* require SSL for Authentication
        if (siteRoot.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            siteRoot = siteRoot.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(config.Current.AlternateSiteRootList))
        {
            var alternateSiteRootList = config
                .Current
                .AlternateSiteRootList
                .Split(';')
                .Select(d => d.Trim());

            _alternateSiteRootList = new HashSet<string>(alternateSiteRootList, StringComparer.OrdinalIgnoreCase);
        }

        var authScheme = BaseConfig.AuthenticationType;

        authBuilder.AddOpenIdConnect(authScheme, options =>
        {
            options.CallbackPath = _callbackPath;
            options.SignedOutCallbackPath = "/";
            options.ResponseType = OpenIdConnectResponseType.IdToken;

            options.Scope.Clear();
            options.Scope.Add(OpenIdConnectScope.OpenIdProfile);
            options.Scope.Add("email");

            // CodeQL [SM03926] We do not restrict issuers to a limited set of tenants for our multi-tenant app
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = false
            };

            options.Events = new OpenIdConnectEvents
            {
                OnAuthenticationFailed = AuthenticationFailed,
                OnRedirectToIdentityProvider = RedirectToIdentityProvider,
                OnAuthorizationCodeReceived = AuthorizationCodeReceived
            };

            Config.ApplyToAuthSecurityOptions(options);
        });
    }

    public override AuthenticatorUI GetUI()
    {
        return new AuthenticatorUI(
            ServicesStrings.MicrosoftAccount_SignInMessage,
            ServicesStrings.MicrosoftAccount_SignInMessage,
            ServicesStrings.MicrosoftAccount_AccountNoun)
        {
            IconImagePath = "~/Content/gallery/img/microsoft-account.svg",
            IconImageFallbackPath = "~/Content/gallery/img/microsoft-account-24x24.png",
        };
    }

    public override ActionResult Challenge(string redirectUrl, AuthenticationPolicy policy)
    {
        return new ChallengeResult(BaseConfig.AuthenticationType, redirectUrl, policy?.GetProperties());
    }

    public override bool IsProviderForIdentity(ClaimsIdentity claimsIdentity)
    {
        Claim issuer = claimsIdentity.FindFirst(V2Claims.Issuer);
        Claim tenant = claimsIdentity.FindFirst(V2Claims.TenantId);
        if (issuer == null || tenant == null)
        {
            return false;
        }

        var expectedIssuer = string.Format(Authority, tenant.Value);
        return string.Equals(issuer.Value, expectedIssuer, StringComparison.OrdinalIgnoreCase);
    }

    public override IdentityInformation GetIdentityInformation(ClaimsIdentity claimsIdentity)
    {
        if (!IsProviderForIdentity(claimsIdentity))
        {
            throw new ArgumentException($"The identity is not authored by {nameof(AzureActiveDirectoryV2Authenticator)}");
        }

        var tenantClaim = claimsIdentity.FindFirst(V2Claims.TenantId);
        if (tenantClaim == null)
        {
            throw new ArgumentException($"External Authentication is missing required claim: {V2Claims.TenantId}");
        }

        var tenantId = tenantClaim.Value;
        string authenticationType = null;
        string identifier = null;
        var idClaim = claimsIdentity.FindFirst(V2Claims.Identifier);
        if (idClaim == null)
        {
            throw new ArgumentException($"External Authentication is missing required claim: '{V2Claims.Identifier}'");
        }

        if (string.Equals(tenantId, PersonalMSATenant, StringComparison.OrdinalIgnoreCase))
        {
            authenticationType = AuthenticationType.MicrosoftAccount;

            // The MSA v2 authentication identifier is returned as 32 character alphanumeric value(padded with 0 and -), 
            // where as the existing Microsoft account identifiers are 16 character wide.
            // For e.g old format: 0ae45d63e22e4a60, newer format: 00000000-0000-0000-0AE4-5D63-E22E4A60
            // We need to format the values into the older format for backwards compatibility
            identifier = idClaim.Value.Replace("-", "").Substring(16).ToLowerInvariant();
        }
        else
        {
            authenticationType = AuthenticationType.AzureActiveDirectory;
            identifier = idClaim.Value;
        }

        var nameClaim = claimsIdentity.FindFirst(V2Claims.Name);
        var emailClaim = claimsIdentity.FindFirst(V2Claims.EmailAddress);
        var preferredUsernameClaim = claimsIdentity.FindFirst(V2Claims.PreferredUsername);
        emailClaim = emailClaim ?? preferredUsernameClaim;
        if (emailClaim == null)
        {
            throw new ArgumentException($"External Authentication is missing required claims: '{V2Claims.EmailAddress}' and '{V2Claims.PreferredUsername}");
        }

        var acrClaim = claimsIdentity.FindFirst(V2Claims.ACR);
        var multiFactorAuthenticated = acrClaim?.Value.Equals(ACR_VALUES.MFA, StringComparison.OrdinalIgnoreCase) ?? false;

        return new IdentityInformation(identifier, nameClaim?.Value, emailClaim.Value, authenticationType, tenantId, multiFactorAuthenticated);
    }

    public override bool SupportsMultiFactorAuthentication()
    {
        // AADv2 supports multi-factor authentication by the use of OpenIdConnect protocol with ACR_VALUES.
        return true;
    }

    // The OpenIdConnect handler throws upon denial of access permissions by the user,
    // catch this exception and continue to the controller where appropriate error handling is done.
    private Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        // All authentication related exceptions will be handled.
        context.HandleResponse();

        var redirectUri = context.Properties?.RedirectUri;

        // Pass the errors as query string parameters
        var queryString = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>(ErrorQueryKeys.Error, context.ProtocolMessage?.Error),
            new KeyValuePair<string, string>(ErrorQueryKeys.ErrorDescription, context.ProtocolMessage?.ErrorDescription)
        };

        var targetUri = UriExtensions.AppendQueryStringToRelativeUri(redirectUri, queryString);
        context.Response.Redirect(targetUri);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Before redirecting for authentication to the provider, append the properties for Multi-Factor Authentication
    /// and configuration settings.
    /// </summary>
    private Task RedirectToIdentityProvider(RedirectContext context)
    {
        if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
        {
            // If sign-out suppression is required
            return Task.CompletedTask;
        }

        var items = context.Properties?.Items;

        // AcrValues token control the multi-factor authentication, when supplied with any, the user set policy for 2FA
        // is enforced. When explicitly set to mfa, the authentication is enforced with multi-factor auth.
        if (items != null && AuthenticationPolicy.TryGetPolicyFromProperties(items, out AuthenticationPolicy policy))
        {
            context.ProtocolMessage.AcrValues = policy.EnforceMultiFactorAuthentication ? ACR_VALUES.MFA : ACR_VALUES.ANY;
            context.ProtocolMessage.LoginHint = policy.Email;
        }
        else
        {
            context.ProtocolMessage.AcrValues = ACR_VALUES.ANY;
        }

        // Set the redirect_uri token for the alternate domains of same gallery instance
        if (_alternateSiteRootList != null && _alternateSiteRootList.Contains(context.Request.Host.Host))
        {
            context.ProtocolMessage.RedirectUri = "https://" + context.Request.Host.Value + _callbackPath;
        }

        // We always want to show the options to select account when signing in and while changing account.
        context.ProtocolMessage.Prompt = SELECT_ACCOUNT;

        return Task.CompletedTask;
    }

    private Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
    {
        // Explicitly set the access_token to null. The access_token is intended for authorized requests to Microsoft Entra ID on
        // behalf of the end user. We do not use this feature. We only use the id_token.
        context.HandleCodeRedemption(accessToken: null, idToken: context.JwtSecurityToken?.RawData);
        return Task.CompletedTask;
    }
}
