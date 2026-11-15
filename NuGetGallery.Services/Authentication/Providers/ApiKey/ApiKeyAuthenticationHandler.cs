// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NuGetGallery.Infrastructure.Authentication;

namespace NuGetGallery.Authentication.Providers.ApiKey;

public class ApiKeyAuthenticationHandler
    : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string FailureMessageKey =
        "NuGetGallery.ApiKeyAuthentication.FailureMessage";

    private readonly AuthenticationService _auth;
    private readonly IFeatureFlagService _featureFlagService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AuthenticationService auth,
        ICredentialBuilder credentialBuilder,
        IFeatureFlagService featureFlagService)
        : base(options, logger, encoder)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));

        // برای حفظ قرارداد سازندهٔ قبلی؛ در منطق فعلی Handler استفاده نمی‌شود.
        _ = credentialBuilder
            ?? throw new ArgumentNullException(nameof(credentialBuilder));

        _featureFlagService = featureFlagService
            ?? throw new ArgumentNullException(nameof(featureFlagService));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                Options.ApiKeyHeaderName, out var headerValues))
        {
            Logger.LogDebug("No API Key header found in request.");
            return AuthenticateResult.NoResult();
        }

        if (headerValues.Count != 1 ||
            string.IsNullOrWhiteSpace(headerValues[0]))
        {
            return AuthenticateResult.Fail("Invalid API Key header.");
        }

        var authUser = await _auth.Authenticate(headerValues[0]!);

        if (authUser is null)
        {
            Logger.LogWarning("No match for API Key.");
            return AuthenticateResult.Fail("Invalid API Key.");
        }

        var credential = authUser.CredentialUsed;
        var user = authUser.User;

        if (!user.MatchesOwnerScope(credential))
        {
            Context.Items[FailureMessageKey] =
                ServicesStrings.ApiKeyNotAuthorized;

            return AuthenticateResult.Fail(
                "API Key not authorized for owner scope.");
        }

        if (credential.IsApiKey() &&
            credential.Expires.HasValue &&
            _featureFlagService.IsApiKeyReductionDateEnabled())
        {
            var effectiveExpiration =
                ApiKeyReductionPolicy.GetEffectiveExpiration(
                    credential.Created, credential.Expires);

            if (effectiveExpiration.HasValue &&
                DateTime.UtcNow >= effectiveExpiration.Value)
            {
                Context.Items[FailureMessageKey] =
                    ServicesStrings.ApiKeyExpiredUnderReductionPolicy;

                return AuthenticateResult.Fail(
                    "API Key expired under reduction policy.");
            }
        }

        var scopes = JsonConvert.SerializeObject(
            credential.Scopes, Formatting.None);

        // از سازندهٔ هویت موجود پروژه استفاده می‌کنیم تا claimهای
        // اصلی کاربر که پیش‌تر ساخته می‌شدند از دست نروند.
        var identity = AuthenticationService.CreateIdentity(
            user,
            Scheme.Name,
            new Claim(Options.ApiKeyClaim, credential.Value),
            new Claim(
                NuGetClaims.CredentialType,
                credential.Type ?? string.Empty),
            new Claim(NuGetClaims.Scope, scopes),
            new Claim(
                NuGetClaims.CredentialKey,
                credential.Key.ToString()));

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // فقط بعد از موفقیت کامل احراز هویت، کاربر جاری را ثبت کن.
        Context.Items[ServicesConstants.CurrentUserOwinEnvironmentKey] =
            authUser;

        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(
        AuthenticationProperties properties)
    {
        Response.ContentType = "text/plain; charset=utf-8";

        if (Request.Headers.ContainsKey(Options.ApiKeyHeaderName))
        {
            // اگر Challenge بدون احراز هویت قبلی فراخوانی شده باشد،
            // علت شکست را محاسبه می‌کند؛ نتیجه در همان درخواست cache می‌شود.
            await AuthenticateAsync();

            Response.StatusCode = StatusCodes.Status403Forbidden;

            var message =
                Context.Items.TryGetValue(
                    FailureMessageKey, out var failureMessage) &&
                failureMessage is string specificMessage
                    ? specificMessage
                    : ServicesStrings.ApiKeyNotAuthorized;

            await Response.WriteAsync(message);
            return;
        }

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers["WWW-Authenticate"] = string.Format(
            CultureInfo.InvariantCulture,
            "ApiKey realm=\"{0}\"",
            Request.Host.Host.Replace("\"", string.Empty));

        await Response.WriteAsync(ServicesStrings.ApiKeyRequired);
    }

    protected override Task HandleForbiddenAsync(
        AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "text/plain; charset=utf-8";

        var message =
            Context.Items.TryGetValue(
                FailureMessageKey, out var failureMessage) &&
            failureMessage is string specificMessage
                ? specificMessage
                : ServicesStrings.ApiKeyNotAuthorized;

        return Response.WriteAsync(message);
    }
}
