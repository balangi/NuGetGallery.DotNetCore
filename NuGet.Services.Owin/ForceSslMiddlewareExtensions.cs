// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.Middleware;

namespace Microsoft.AspNetCore.Builder;

public static class ForceSslMiddlewareExtensions
{
    /// <summary>
    /// فعال‌سازی میدل‌ویر فورس SSL با پورت پیش‌فرض 443
    /// </summary>
    public static IApplicationBuilder UseForceSsl(this IApplicationBuilder appBuilder)
    {
        return UseForceSsl(appBuilder, 443);
    }

    /// <summary>
    /// فعال‌سازی میدل‌ویر فورس SSL با پورت مشخص
    /// </summary>
    public static IApplicationBuilder UseForceSsl(this IApplicationBuilder appBuilder, int sslPort)
    {
        return UseForceSsl(appBuilder, sslPort, Enumerable.Empty<string>());
    }

    /// <summary>
    /// فعال‌سازی میدل‌ویر فورس SSL با پورت مشخص و مسیرهای استثنا
    /// </summary>
    public static IApplicationBuilder UseForceSsl(
        this IApplicationBuilder appBuilder,
        int sslPort,
        IEnumerable<string> excludedPaths)
    {
        ArgumentNullException.ThrowIfNull(appBuilder);

        return appBuilder.UseMiddleware<ForceSslMiddleware>(
            sslPort,
            excludedPaths ?? Enumerable.Empty<string>());
    }
}
