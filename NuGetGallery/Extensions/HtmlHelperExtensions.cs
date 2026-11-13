using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using NuGet.Services.Licenses;

namespace NuGetGallery.Extensions
{
    public static class HtmlHelperExtensions
    {
        public static string GetColumnClasses(this IHtmlHelper html, string screenSize, int? columns)
        {
            const int totalColumns = 12;
            if (!columns.HasValue)
            {
                return string.Empty;
            }

            var offset = (totalColumns - columns.Value) / 2;
            var columnsClass = $"col-{screenSize}-{columns.Value}";
            var offsetClass = offset == 0 ? string.Empty : $" col-{screenSize}-offset-{offset}";

            return $"{columnsClass} {offsetClass}".Trim();
        }

        public static bool IsLicenseOrException(this IHtmlHelper html, CompositeLicenseExpressionSegment segment)
        {
            return segment.Type == CompositeLicenseExpressionSegmentType.LicenseIdentifier 
                || segment.Type == CompositeLicenseExpressionSegmentType.ExceptionIdentifier;
        }

        public static bool IsLicense(this IHtmlHelper html, CompositeLicenseExpressionSegment segment)
        {
            return segment.Type == CompositeLicenseExpressionSegmentType.LicenseIdentifier;
        }

        public static bool IsException(this IHtmlHelper html, CompositeLicenseExpressionSegment segment)
        {
            return segment.Type == CompositeLicenseExpressionSegmentType.ExceptionIdentifier;
        }

        public static IHtmlContent ImageFallback(this IHtmlHelper html, string url)
        {
            return new HtmlString($"onerror=\"this.src='{JavaScriptEncoder.Default.Encode(url)}'; this.onerror = null;\"");
        }

        public static IHtmlContent PackageImageFallback(this IHtmlHelper html)
        {
            return new HtmlString("onerror=\"this.className='package-icon img-responsive package-default-icon'; this.onerror = null;\"");
        }
    }
}
