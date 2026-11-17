// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Versioning;
using NuGetGallery.Services.Helpers;

namespace NuGetGallery.Infrastructure.Constraints;

/// <summary>Accepts a NuGet version, or the absolute-latest route URL.</summary>
public sealed class VersionRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext httpContext,
        IRouter route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (routeDirection == RouteDirection.UrlGeneration)
        {
            return true;
        }

        if (values == null || !values.TryGetValue(routeKey, out var versionValue))
        {
            return true;
        }

        if (versionValue == null)
        {
            return true;
        }

        var versionText = Convert.ToString(versionValue);
        if (string.IsNullOrEmpty(versionText))
        {
            return true;
        }

        var absoluteLatestUrl = LatestPackageRouteVerifier.SupportedRoutes.AbsoluteLatestUrlString;
        if (string.Equals(versionText, absoluteLatestUrl, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        return NuGetVersion.TryParse(versionText, out _);
    }
}
