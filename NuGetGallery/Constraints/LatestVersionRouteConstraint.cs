// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NuGet.Versioning;
using NuGetGallery.Services.Helpers;

namespace NuGetGallery.Infrastructure.Constraints
{
    /// <summary>Accepts NuGet versions and the supported latest-version route URLs.</summary>
    public sealed class LatestVersionRouteConstraint : IRouteConstraint
    {
        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
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

            if (IsSupportedLatestRoute(route, httpContext))
            {
                return true;
            }

            return NuGetVersion.TryParse(versionText, out _);
        }

        private static bool IsSupportedLatestRoute(IRouter route, HttpContext httpContext)
        {
            var supportedRoutes = LatestPackageRouteVerifier.SupportedRoutes;
            return IsSupportedUrl(GetRouteTemplate(route), supportedRoutes.LatestUrlString,
                    supportedRoutes.LatestUrlWithPreleaseString,
                    supportedRoutes.LatestUrlWithPreleaseAndVersionString)
                || IsSupportedUrl(httpContext?.Request?.Path.Value, supportedRoutes.LatestUrlString,
                    supportedRoutes.LatestUrlWithPreleaseString,
                    supportedRoutes.LatestUrlWithPreleaseAndVersionString);
        }

        private static string GetRouteTemplate(IRouter route)
        {
            return (route as Route)?.RouteTemplate?.RawText;
        }

        private static bool IsSupportedUrl(string url, params string[] supportedUrls)
        {
            if (url == null)
            {
                return false;
            }

            foreach (var supportedUrl in supportedUrls)
            {
                if (string.Equals(url, supportedUrl, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
