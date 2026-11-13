using Microsoft.AspNetCore.Mvc;

namespace NuGetGallery.Components
{
    public class PagerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IPreviousNextPager pager)
        {
            if (pager == null || (!pager.HasNextPage && !pager.HasPreviousPage))
            {
                // در صورتی که صفحه‌ای وجود نداشته باشد، خروجی خالی رندر می‌شود
                return Content(string.Empty);
            }

            return View(pager);
        }
    }
}
