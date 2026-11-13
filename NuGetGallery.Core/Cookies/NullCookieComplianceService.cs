// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace NuGetGallery.Cookies;

/// <summary>
/// Default, no-op instance of the cookie compliance service, used when no shim is registered.
/// </summary>
public class NullCookieComplianceService : ICookieComplianceService
{
    public Task<bool> CanWriteAnalyticsCookiesAsync(HttpRequest request) => Task.FromResult(false);

    public Task<bool> CanWriteSocialMediaCookiesAsync(HttpRequest request) => Task.FromResult(false);

    public Task<bool> CanWriteAdvertisingCookiesAsync(HttpRequest request) => Task.FromResult(false);
}