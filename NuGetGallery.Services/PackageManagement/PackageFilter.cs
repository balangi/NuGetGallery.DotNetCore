// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using NuGet.Services.Entities;
using NuGetGallery.Services.Helpers;

namespace NuGetGallery.Services;

public class PackageFilter : IPackageFilter
{
    private readonly IPackageService _packageService;

    public PackageFilter(IPackageService packageService)
    {
        _packageService = packageService
            ?? throw new ArgumentNullException(nameof(packageService));
    }

    /// <inheritdoc />
    public Package GetFiltered(
        IReadOnlyCollection<Package> packages,
        PackageFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(context);

        Package result = null;

        var routeUrl = context.Endpoint is RouteEndpoint routeEndpoint
            ? routeEndpoint.RoutePattern.RawText
            : null;

        var version = context.Version;

        if (string.Equals(
                version,
                LatestPackageRouteVerifier.SupportedRoutes.AbsoluteLatestUrlString,
                StringComparison.InvariantCultureIgnoreCase))
        {
            // The user is looking for the absolute latest version and not an exact version.
            result = packages.FirstOrDefault(p => p.IsLatestSemVer2);
        }

        result = result
            ?? _packageService.FilterExactPackage(packages, version);

        if (LatestPackageRouteVerifier.IsLatestRoute(
                routeUrl,
                out var preRelease))
        {
            result = result
                ?? _packageService.FilterLatestPackageBySuffix(
                    packages,
                    version,
                    preRelease);
        }

        // The package still wasn't found, so get the latest version.
        result = result
            ?? _packageService.FilterLatestPackage(
                packages,
                SemVerLevelKey.SemVer2,
                allowPrerelease: true);

        return result;
    }
}
