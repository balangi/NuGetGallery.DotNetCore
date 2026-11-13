using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace NuGetGallery
{
    /// <summary>Service and middleware setup replacing the legacy Web API config.</summary>
    public static class WebApiConfig
    {
        public static IMvcBuilder Register(IServiceCollection services)
        {
            services.AddRouting();
            return services
                .AddControllers()
                .AddOData(NuGetODataConfig.Configure);
        }

        /// <summary>Call after service registration and endpoint middleware setup.</summary>
        public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGalleryRoutes();
        }
    }
}
