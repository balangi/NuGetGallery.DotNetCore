// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NuGetGallery.Configuration;
using System.Security.Claims;

namespace NuGetGallery.Authentication.Providers.AzureActiveDirectory;

public class AzureActiveDirectoryAuthenticator : Authenticator<AzureActiveDirectoryAuthenticatorConfiguration>
{
    public static readonly string DefaultAuthenticationType = "AzureActiveDirectory";
    public static readonly string ClaimTypeName = "name";

    protected override void AttachToApp(IGalleryConfigurationService config, AuthenticationBuilder authBuilder)
    {
        // Fetch site root from configuration
        var siteRoot = config.Current.SiteRoot.TrimEnd('/') + "/";

        // We *always* require SSL for Microsoft Entra ID
        if (siteRoot.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            siteRoot = siteRoot.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
        }

        var authScheme = BaseConfig.AuthenticationType;

        authBuilder.AddOpenIdConnect(authScheme, options =>
        {
            options.CallbackPath = "/users/account/authenticate/return";
            options.SignedOutCallbackPath = "/";

            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = notification =>
                {
                    if (notification.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
                    {
                        // We never intend to sign out at the federated identity. Suppress the redirect.
                        notification.HandleResponse();
                    }

                    return Task.CompletedTask;
                }
            };

            // فراخوانی متد کانفیگ برای اعمال ClientId, Authority و Issuer
            Config.ApplyToAuthSecurityOptions(options);
        });
    }

    public override AuthenticatorUI GetUI()
    {
        return new AuthenticatorUI(
            ServicesStrings.AzureActiveDirectory_SignInMessage,
            ServicesStrings.AzureActiveDirectory_RegisterMessage,
            ServicesStrings.AzureActiveDirectory_AccountNoun)
        {
            ShowOnLoginPage = Config.ShowOnLoginPage
        };
    }

    public override bool IsProviderForIdentity(ClaimsIdentity claimsIdentity)
    {
        // If the issuer of the claims identity is same as that of the issuer for current authenticator then this is the author.
        var firstClaim = claimsIdentity?.Claims?.FirstOrDefault();
        if (firstClaim != null && string.Equals(firstClaim.Issuer, Config.Issuer, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return base.IsProviderForIdentity(claimsIdentity);
    }

    public override ActionResult Challenge(string redirectUrl, AuthenticationPolicy policy)
    {
        var properties = policy?.GetProperties() as IDictionary<string, string>;

        // فراخوانی با امضای (string provider, string redirectUri, IDictionary<string, string> properties)
        return new ChallengeResult(BaseConfig.AuthenticationType, redirectUrl, properties);
    }

    public override IdentityInformation GetIdentityInformation(ClaimsIdentity claimsIdentity)
    {
        return ClaimsExtensions.GetIdentityInformation(
            claimsIdentity,
            DefaultAuthenticationType,
            ClaimTypes.NameIdentifier,
            ClaimTypeName,
            ClaimTypes.Name);
    }
}
