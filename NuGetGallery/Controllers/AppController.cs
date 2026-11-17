// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NuGet.Services.Entities;
using NuGetGallery.Cookies;
using System.Security.Claims;

namespace NuGetGallery;

public abstract partial class AppController : Controller
{
    private ICookieExpirationService _cookieExpirationService;
    private IFeatureFlagService _featureFlagService;

    public NuGetContext NuGetContext { get; }

    public new ClaimsPrincipal User => base.User as ClaimsPrincipal;

    /// <summary>
    /// This method is used for the unit test.
    /// </summary>
    public void SetCookieExpirationService(ICookieExpirationService cookieExpirationService)
    {
        _cookieExpirationService = cookieExpirationService;
    }

    /// <summary>
    /// This method is used for the unit test.
    /// </summary>
    public void SetFeatureFlagsService(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService;
    }

    protected AppController()
    {
        NuGetContext = new NuGetContext(this);
    }

    protected internal virtual T GetService<T>()
    {
        return HttpContext.RequestServices.GetRequiredService<T>();
    }

    protected internal virtual ICookieExpirationService CookieExpirationService =>
        _cookieExpirationService ??= GetService<ICookieExpirationService>();

    protected internal virtual IFeatureFlagService FeatureFlagService =>
        _featureFlagService ??= GetService<IFeatureFlagService>();

    protected internal virtual User GetCurrentUser()
    {
        var userService = GetService<IUserService>();
        var username = User?.FindFirstValue(ClaimTypes.Name) ?? User?.Identity?.Name;
        return username != null ? userService?.FindByUsername(username) : null;
    }

    protected internal virtual IActionResult SafeRedirect(string returnUrl)
    {
        var homeUrl = Url.Action("Index", "Home") ?? "/";
        // در صورت نیاز به کست صریح یا انطباق نوع نتیجه
        return (IActionResult)new SafeRedirectResult(returnUrl, homeUrl);
    }

    protected internal JsonResult Json(int statusCode, object obj)
    {
        HttpContext.Response.StatusCode = statusCode;
        return base.Json(obj);
    }

    protected internal JsonResult Json(int statusCode)
    {
        return Json(statusCode, new { });
    }

    /// <summary>
    /// Called before the action method is invoked.
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var isChildAction = filterContext.ActionDescriptor.DisplayName?.Contains("Child") ?? false;
        if (!isChildAction)
        {
            var clientCulture = HttpContext.Request.DetermineClientCulture();
            if (clientCulture != null)
            {
                Thread.CurrentThread.CurrentCulture = clientCulture;
                Thread.CurrentThread.CurrentUICulture = clientCulture;
            }
        }

        base.OnActionExecuting(filterContext);
    }

    /// <summary>
    /// Called after the action method is invoked.
    /// </summary>
    public override void OnActionExecuted(ActionExecutedContext filterContext)
    {
        SetCookieCompliance(filterContext);
        SetBannerFeatureFlag();

        base.OnActionExecuted(filterContext);
    }

    private void SetCookieCompliance(ActionExecutedContext filterContext)
    {
        if (filterContext.HttpContext.Items[ServicesConstants.CookieComplianceCanWriteAnalyticsCookies] == null
            || (bool)filterContext.HttpContext.Items[ServicesConstants.CookieComplianceCanWriteAnalyticsCookies] == false)
        {
            ViewBag.CanWriteAnalyticsCookies = false;
            CookieExpirationService.ExpireAnalyticsCookies(filterContext.HttpContext);
        }
        else
        {
            ViewBag.CanWriteAnalyticsCookies = true;
        }
    }

    private void SetBannerFeatureFlag()
    {
        if (FeatureFlagService.IsDisplayBannerEnabled())
        {
            ViewBag.DisplayBanner = true;
        }
        else
        {
            ViewBag.DisplayBanner = false;
        }
    }
}
