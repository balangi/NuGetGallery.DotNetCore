// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NuGetGallery.Configuration;

namespace NuGetGallery.Authentication.Providers.Cookie;

public class LocalUserAuthenticator : Authenticator
{
    protected override void AttachToApp(
        IGalleryConfigurationService config,
        AuthenticationBuilder authBuilder)
    {
        var cookieSecurePolicy = config.Current.RequireSSL
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.None;

        authBuilder.AddCookie(AuthenticationTypes.LocalUser, options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;

            options.LoginPath = "/users/account/LogOn";
            options.ExpireTimeSpan = TimeSpan.FromHours(6);
            options.SlidingExpiration = true;

            BaseConfig.ApplyToAuthSecurityOptions(options);
        });
    }

    protected internal override AuthenticatorConfiguration CreateConfigObject()
    {
        return new AuthenticatorConfiguration
        {
            AuthenticationType = AuthenticationTypes.LocalUser,
            Enabled = false
        };
    }
}
