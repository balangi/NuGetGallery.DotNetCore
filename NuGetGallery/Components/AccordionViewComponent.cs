using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using NuGetGallery.ViewModels;

namespace NuGetGallery.Components
{
    public class AccordionViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(
            string groupName,
            string title,
            string subtitle = null,
            IHtmlContent actions = null,
            IHtmlContent content = null,
            bool enabled = true,
            string formModelStatePrefix = null,
            bool expanded = false)
        {
            return InvokeWithHtml(
                groupName,
                new HtmlString(title ?? string.Empty),
                string.IsNullOrEmpty(subtitle) ? null : new HtmlString(subtitle),
                actions,
                content,
                enabled,
                formModelStatePrefix,
                expanded);
        }

        public IViewComponentResult InvokeWithHtml(
            string groupName,
            IHtmlContent title,
            IHtmlContent subtitle = null,
            IHtmlContent actions = null,
            IHtmlContent content = null,
            bool enabled = true,
            string formModelStatePrefix = null,
            bool expanded = false)
        {
            // جایگزین HttpContext.Current.Items قدیمی
            string dataKey = $"___AccordionCounter_{groupName}";
            int lastId = 0;

            if (HttpContext.Items.TryGetValue(dataKey, out var currentVal) && currentVal is int val)
            {
                lastId = val;
            }

            int id = lastId + 1;
            HttpContext.Items[dataKey] = id;

            string name = $"{groupName}-{id}";

            var model = new AccordionBarViewModel
            {
                GroupName = groupName,
                Title = title,
                Subtitle = subtitle,
                Actions = actions,
                Content = content,
                Enabled = enabled,
                FormModelStatePrefix = formModelStatePrefix,
                Expanded = expanded,
                ItemId = $"item-{name}",
                ActionsId = $"{name}-actions",
                ContentDropDownId = $"{name}-content",
                ContentHiddenClass = expanded ? string.Empty : "hidden"
            };

            return View(model);
        }
    }
}
