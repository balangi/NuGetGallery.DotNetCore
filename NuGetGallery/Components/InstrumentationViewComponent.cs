using Microsoft.AspNetCore.Mvc;
using NuGetGallery.Configuration;

namespace NuGetGallery.Components
{
    public class InstrumentationViewComponent : ViewComponent
    {
        private readonly IGalleryConfigurationService _config;

        public InstrumentationViewComponent(IGalleryConfigurationService config)
        {
            _config = config;
        }

        public IViewComponentResult Invoke(bool canWriteAnalyticsCookies)
        {
            if (!canWriteAnalyticsCookies)
            {
                return Content(string.Empty);
            }

            var model = new InstrumentationViewModel
            {
                AppInsightsKey = _config.Current.AppInsightsInstrumentationKey,
                SamplingPercentage = _config.Current.AppInsightsSamplingPercentage,
                TenantKey = _config.Current.InternalMicrosoftTenantKey
            };

            return View(model);
        }
    }

    public class InstrumentationViewModel
    {
        public string AppInsightsKey { get; set; }
        public int SamplingPercentage { get; set; }
        public string TenantKey { get; set; }
    }
}
