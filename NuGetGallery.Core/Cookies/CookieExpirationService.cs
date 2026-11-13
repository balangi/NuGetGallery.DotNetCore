// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace NuGetGallery.Cookies;

public class CookieExpirationService : ICookieExpirationService
{
    // Google Analytics cookies
    private static readonly string[] GoogleAnalyticsCookies = new[]
    {
        "_ga",
        "_gid",
        "_gat",
    };

    // Application Insights cookies
    private static readonly string[] ApplicationInsightsCookies = new[]
    {
        "ai_user",
        "ai_session",
    };

    public void ExpireAnalyticsCookies(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        Array.ForEach(GoogleAnalyticsCookies, cookieName => ExpireCookieByName(httpContext, cookieName));
        Array.ForEach(ApplicationInsightsCookies, cookieName => ExpireCookieByName(httpContext, cookieName));
    }

    public void ExpireSocialMediaCookies(HttpContext httpContext) { }
    public void ExpireAdvertisingCookies(HttpContext httpContext) { }

    public void ExpireCookieByName(HttpContext httpContext, string cookieName, string? domain = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (string.IsNullOrEmpty(cookieName))
        {
            throw new ArgumentException(CoreStrings.ArgumentCannotBeNullOrEmpty, nameof(cookieName));
        }

        var request = httpContext.Request;
        var response = httpContext.Response;

        if (request == null || response == null)
        {
            return;
        }

        // بررسی اینکه آیا کوکی در ریکوئست ورودی وجود دارد یا خیر
        if (request.Cookies.ContainsKey(cookieName))
        {
            if (!string.IsNullOrEmpty(domain))
            {
                var options = new CookieOptions
                {
                    Domain = domain,
                    Path = "/"
                };
                response.Cookies.Delete(cookieName, options);
            }
            else
            {
                response.Cookies.Delete(cookieName);
            }
        }
    }
}
