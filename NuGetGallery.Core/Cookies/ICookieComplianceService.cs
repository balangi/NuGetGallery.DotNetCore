// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace NuGetGallery.Cookies;

/// <summary>
/// Cookie compliance service, used to comply with EU privacy laws.
/// </summary>
public interface ICookieComplianceService
{
    /// <summary>
    /// Determine whether it's allowed to write analytics cookies.
    /// </summary>
    /// <returns>True if it's allowed.</returns>
    Task<bool> CanWriteAnalyticsCookiesAsync(HttpRequest request);

    /// <summary>
    /// Determine whether it's allowed to write social media cookies.
    /// </summary>
    /// <returns>True if it's allowed</returns>
    Task<bool> CanWriteSocialMediaCookiesAsync(HttpRequest request);

    /// <summary>
    /// Determine whether it's allowed to write advertising cookies.
    /// </summary>
    /// <returns>True if it's allowed.</returns>
    Task<bool> CanWriteAdvertisingCookiesAsync(HttpRequest request);
}