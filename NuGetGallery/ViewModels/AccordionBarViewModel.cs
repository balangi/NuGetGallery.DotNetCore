using Microsoft.AspNetCore.Html;

namespace NuGetGallery.ViewModels
{
    public class AccordionBarViewModel
    {
        public string GroupName { get; set; } = string.Empty;
        public IHtmlContent Title { get; set; }
        public IHtmlContent Subtitle { get; set; }
        public bool Enabled { get; set; } = true;
        public string FormModelStatePrefix { get; set; }
        public IHtmlContent Actions { get; set; }
        public IHtmlContent Content { get; set; }
        public bool Expanded { get; set; } = false;

        // فیلدهای تولیدی در زمان اجرا
        public string ItemId { get; set; }
        public string ActionsId { get; set; }
        public string ContentDropDownId { get; set; }
        public string ContentHiddenClass { get; set; } = "hidden";
    }
}
