using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace NuGetGallery
{
    /// <summary>
    /// Endpoint routing migration scaffold. OData read routes are supplied by
    /// attribute/conventional OData controllers and mapped through MapControllers.
    /// Add application-specific read actions/controllers to expose package data.
    /// </summary>
    public static class Routes
    {
        public static IEndpointRouteBuilder MapGalleryRoutes(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapControllers();
            MapReadRoutes(endpoints);
            MapUnimplementedRoutes(endpoints);
            return endpoints;
        }

        /// <summary>Non-OData read endpoints that are safe defaults for the scaffold.</summary>
        private static void MapReadRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }))
                .WithName("GalleryHealth");
            // OData feed reads (GET /api/v1/Packages and /api/v2/Packages,
            // plus their query options) are handled by OData controllers.
        }

        /// <summary>
        /// Catch-all migration placeholder; replace with real create/update/delete
        /// handlers and any remaining legacy routes. Keeping it on its own prefix
        /// avoids intercepting OData feed endpoints.
        /// </summary>
        private static void MapUnimplementedRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(
                "/migration-stubs/{**path}",
                new[] { "GET", "POST", "PUT", "PATCH", "DELETE" },
                (string? path) => Results.Problem(
                    title: "Route not migrated",
                    detail: $"Implement the legacy route '{path ?? string.Empty}' in the ASP.NET Core application.",
                    statusCode: StatusCodes.Status501NotImplemented));
        }
    }
}
