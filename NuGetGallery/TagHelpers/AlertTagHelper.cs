using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace NuGetGallery.TagHelpers
{
    [HtmlTargetElement("gallery-alert")]
    public class AlertTagHelper : TagHelper
    {
        public string Type { get; set; } = "info"; // info, warning, danger, success
        public string Icon { get; set; } = "Info";
        public bool IsAlertRole { get; set; } = false;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.Attributes.SetAttribute("class", $"icon-text alert alert-brand-{Type}");

            if (IsAlertRole || Type == "danger")
            {
                output.Attributes.SetAttribute("role", "alert");
                output.Attributes.SetAttribute("aria-live", "assertive");
            }

            var childContent = await output.GetChildContentAsync();

            var iconHtml = $"<i class=\"ms-Icon ms-Icon--{Icon}\" aria-hidden=\"true\"></i> ";
            output.Content.SetHtmlContent(iconHtml);
            output.Content.AppendHtml(childContent);
        }
    }
}

// نحوه استفاده در ویوها:
// <!-- به جای @ViewHelpers.AlertWarning(...) -->
// <gallery-alert type="warning" icon="Warning">
//     NuGet.org password login is no longer supported. Please use a Microsoft account.
// </gallery-alert>
