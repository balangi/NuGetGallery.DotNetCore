// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NuGetGallery.Configuration;

namespace NuGetGallery.Authentication.Providers.MicrosoftAccount;

public class MicrosoftAccountAuthenticator : Authenticator<MicrosoftAccountAuthenticatorConfiguration>
{
    public static readonly string DefaultAuthenticationType = "MicrosoftAccount";

    protected override void AttachToApp(IGalleryConfigurationService config, AuthenticationBuilder authBuilder)
    {
        authBuilder.AddMicrosoftAccount(BaseConfig.AuthenticationType, options =>
        {
            // اسکوپ‌های معادل در ASP.NET Core
            options.Scope.Add("https://graph.microsoft.com/user.read");

            // اعمال تنظیمات ClientId و ClientSecret از طریق کلاس Config
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
            // مسیرهای ثابت در ASP.NET Core معمولاً بدون ~ هستند (بسته به تنظیمات Static Files)
            IconImagePath = "/Content/gallery/img/microsoft-account.svg",
            IconImageFallbackPath = "/Content/gallery/img/microsoft-account-24x24.png",
        };
    }

    public override ActionResult Challenge(string redirectUrl, AuthenticationPolicy policy = null)
    {
        // استفاده از کلاس ChallengeResult سفارشی که قبلاً برای Core نوشتیم
        return new ChallengeResult(BaseConfig.AuthenticationType, redirectUrl, policy?.GetProperties());
    }

    public override IdentityInformation GetIdentityInformation(ClaimsIdentity claimsIdentity)
    {
        return ClaimsExtensions.GetIdentityInformation(claimsIdentity, DefaultAuthenticationType);
    }
}
