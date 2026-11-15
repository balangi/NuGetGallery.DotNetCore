// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using NuGetGallery.Configuration;
using System.ComponentModel;

namespace NuGetGallery.Authentication.Providers;

public class AuthenticatorConfiguration
{
    [DefaultValue(false)]
    public bool Enabled { get; set; }

    public string AuthenticationType { get; set; }

    public IDictionary<string, string> GetConfigValues()
    {
        return ConfigurationService.GetConfigProperties(this)
            .ToDictionary(
                p => string.IsNullOrEmpty(p.DisplayName) ? p.Name : p.DisplayName,
                p => p.GetValue(this).ToStringSafe());
    }

    /// <summary>
    /// در ASP.NET Core گزینه‌های احراز هویت از AuthenticationSchemeOptions ارث‌بری می‌کنند.
    /// نام متد نیز به جهت خروج از OWIN، ترجیحاً ApplyToAuthSecurityOptions نام‌گذاری می‌شود.
    /// </summary>
    public virtual void ApplyToAuthSecurityOptions(AuthenticationSchemeOptions options)
    {
        // در ASP.NET Core نام احراز هویت (AuthenticationScheme) مستقیماً روی options ست نمی‌شود
        // و هنگام ثبت در DI به متدهای .AddOpenIdConnect(AuthenticationType, ...) پاس داده می‌شود.
        // اما کلاس‌های فرزند در صورت نیاز این متد را برای فیلدهای اختصاصی خود override می‌کنند.
    }

    // برای حفظ سازگاری backward-compatibility با کدهای صدا زننده قبلی:
    [System.Obsolete("Use ApplyToAuthSecurityOptions instead.")]
    public virtual void ApplyToOwinSecurityOptions(AuthenticationSchemeOptions options)
    {
        ApplyToAuthSecurityOptions(options);
    }
}
