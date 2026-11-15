using Microsoft.AspNetCore.Http;

namespace NuGetGallery.Services;

public class PackageFilterContext
{
    // استفاده از Endpoint به عنوان جایگزین عمومی برای شناسایی روت
    public Endpoint Endpoint { get; }

    public string Version { get; }

    public PackageFilterContext(Endpoint endpoint, string version)
    {
        Endpoint = endpoint;
        Version = version;
    }
}
