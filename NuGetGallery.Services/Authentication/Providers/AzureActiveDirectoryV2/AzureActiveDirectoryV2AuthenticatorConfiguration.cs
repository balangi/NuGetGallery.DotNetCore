// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Globalization;

namespace NuGetGallery.Authentication.Providers.AzureActiveDirectoryV2;

public class AzureActiveDirectoryV2AuthenticatorConfiguration : AuthenticatorConfiguration
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }

    public AzureActiveDirectoryV2AuthenticatorConfiguration()
    {
        AuthenticationType = AzureActiveDirectoryV2Authenticator.DefaultAuthenticationType;
    }

    /// <summary>
    /// اعمال تنظیمات احراز هویت اختصاصی AAD V2 بر روی گزینه‌های OpenIdConnect
    /// </summary>
    public override void ApplyToAuthSecurityOptions(AuthenticationSchemeOptions options)
    {
        base.ApplyToAuthSecurityOptions(options);

        if (options is OpenIdConnectOptions openIdOptions)
        {
            // در ASP.NET Core تمام اسکیماها به طور پیش‌فرض حالت Passive دارند
            // و تنها با ChallengeResult صریح فراخوانی می‌شوند.

            if (string.IsNullOrEmpty(TenantId))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    ServicesStrings.MissingRequiredConfigurationValue,
                    "Auth.CommonAuth.TenantId"));
            }

            if (string.IsNullOrEmpty(ClientId))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    ServicesStrings.MissingRequiredConfigurationValue,
                    "Auth.CommonAuth.ClientId"));
            }

            openIdOptions.ClientId = ClientId;
            openIdOptions.Authority = string.Format(
                CultureInfo.InvariantCulture,
                AzureActiveDirectoryV2Authenticator.Authority,
                TenantId);
        }
    }

    [Obsolete("Use ApplyToAuthSecurityOptions instead.")]
    public override void ApplyToOwinSecurityOptions(AuthenticationSchemeOptions options)
    {
        ApplyToAuthSecurityOptions(options);
    }
}
