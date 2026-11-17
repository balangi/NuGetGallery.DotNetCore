// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData;

namespace NuGetGallery;

/// <summary>
/// Service and middleware setup replacing the legacy Web API configuration.
/// </summary>
public static class WebApiConfig
{
    public static IMvcBuilder Register(IServiceCollection services)
    {
        services.AddRouting();

        return services
            .AddControllers()
            .AddOData(options =>
            {
                options.Select()
                       .Filter()
                       .OrderBy()
                       .Expand()
                       .Count()
                       .SetMaxTop(100);
            });
    }

    /// <summary>
    /// Maps MVC and OData controller endpoints.
    /// Call this method after service registration.
    /// </summary>
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        return endpoints;
    }
}
