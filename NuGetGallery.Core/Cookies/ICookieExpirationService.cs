// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace NuGetGallery.Cookies;

/// <summary>
/// Cookie expiration service, used to expire cookies.
/// </summary>
public interface ICookieExpirationService
{
    /// <summary>
    /// The function is used to expire analytics cookies.
    /// </summary>
    /// <param name="httpContext">The httpContext.</param>
    void ExpireAnalyticsCookies(HttpContext httpContext);

    /// <summary>
    /// The function is used to expire social media cookies.
    /// </summary>
    /// <param name="httpContext">The httpContext.</param>
    void ExpireSocialMediaCookies(HttpContext httpContext);

    /// <summary>
    /// The function is used to expire advertising cookies.
    /// </summary>
    /// <param name="httpContext">The httpContext.</param>
    void ExpireAdvertisingCookies(HttpContext httpContext);
}