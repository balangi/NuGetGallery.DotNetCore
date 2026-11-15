// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Globalization;

namespace NuGetGallery.Authentication.Providers.AzureActiveDirectory;

public class AzureActiveDirectoryAuthenticatorConfiguration : AuthenticatorConfiguration
{
    public string ClientId { get; set; }
    public string Authority { get; set; }
    public string Issuer { get; set; }
    public bool ShowOnLoginPage { get; set; }

    public AzureActiveDirectoryAuthenticatorConfiguration()
    {
        AuthenticationType = AzureActiveDirectoryAuthenticator.DefaultAuthenticationType;
    }

    /// <summary>
    /// پیکربندی تنظیمات احراز هویت در ASP.NET Core
    /// </summary>
    public override void ApplyToAuthSecurityOptions(AuthenticationSchemeOptions options)
    {
        base.ApplyToAuthSecurityOptions(options);

        if (options is OpenIdConnectOptions opts)
        {
            // در ASP.NET Core تمام اسکیماها به طور پیش‌فرض حالت Passive دارند
            // و تنها زمانی فعال می‌شوند که به صورت صریح با ChallengeResult فراخوانی شوند.

            // Make sure ClientId is configured
            if (string.IsNullOrEmpty(ClientId))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    ServicesStrings.MissingRequiredConfigurationValue,
                    "Auth.AzureActiveDirectory.ClientId"));
            }

            opts.ClientId = ClientId;

            // Make sure Authority is configured
            if (string.IsNullOrEmpty(Authority))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    ServicesStrings.MissingRequiredConfigurationValue,
                    "Auth.AzureActiveDirectory.Authority"));
            }

            opts.Authority = Authority;

            // Make sure Issuer is configured
            if (string.IsNullOrEmpty(Issuer))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    ServicesStrings.MissingRequiredConfigurationValue,
                    "Auth.AzureActiveDirectory.Issuer"));
            }

            // تنظیم IssuerValidator جهت بررسی معتبر بودن صادرکننده توکن
            opts.TokenValidationParameters.ValidIssuer = Issuer;
            opts.TokenValidationParameters.ValidateIssuer = true;
        }
    }

    [Obsolete("Use ApplyToAuthSecurityOptions instead.")]
    public override void ApplyToOwinSecurityOptions(AuthenticationSchemeOptions options)
    {
        ApplyToAuthSecurityOptions(options);
    }
}
