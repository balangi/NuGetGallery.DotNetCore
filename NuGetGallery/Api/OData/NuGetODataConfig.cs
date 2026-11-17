// ASP.NET Core OData registration shared by the feed versions.

using Microsoft.AspNetCore.OData;
using System.ComponentModel.DataAnnotations;

namespace NuGetGallery;

/// <summary>Shared OData configuration for the NuGet feed endpoints.</summary>
public static class NuGetODataConfig
{
    public static void Configure(ODataOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100);
        NuGetODataV1FeedConfig.Register(options);
        NuGetODataV2FeedConfig.Register(options);
    }
}

/// <summary>
/// Migration DTO used to build the feed EDMs. Replace or map this type to the
/// application's package projection as appropriate.
/// </summary>
public class NuGetODataPackage
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public DateTime Published { get; set; }
    public long DownloadCount { get; set; }
}
