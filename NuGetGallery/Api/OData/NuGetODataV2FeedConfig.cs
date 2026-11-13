using System;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;

namespace NuGetGallery
{
    /// <summary>OData v2 feed route and EDM registration.</summary>
    public static class NuGetODataV2FeedConfig
    {
        public const string RoutePrefix = "api/v2";

        public static void Register(ODataOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.AddRouteComponents(RoutePrefix, CreateModel());
        }

        public static Microsoft.OData.Edm.IEdmModel CreateModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<NuGetODataPackage>("Packages");
            return builder.GetEdmModel();
        }
    }
}
