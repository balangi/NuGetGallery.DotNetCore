using Microsoft.AspNetCore.Mvc;

namespace NuGetGallery;

public partial class ErrorsController : AppController
{
    // در ASP.NET Core از [HttpGet] و [HttpHead] استفاده می‌شود
    [HttpGet]
    [HttpHead]
    public virtual IActionResult BadRequest()
    {
        return View();
    }

    [HttpGet]
    [HttpHead]
    public virtual IActionResult NotFound()
    {
        return View();
    }

    [HttpGet]
    [HttpHead]
    public virtual IActionResult InternalError()
    {
        return View();
    }

    [HttpGet]
    [HttpHead]
    public virtual IActionResult ReadOnlyMode()
    {
        return View();
    }
}
