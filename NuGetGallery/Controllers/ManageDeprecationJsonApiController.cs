// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGetGallery.Auditing;
using NuGetGallery.RequestModels;
using System.Net;

namespace NuGetGallery;

[Authorize] // جایگزینی [UIAuthorize] با استاندارد ASP.NET Core (یا TypeFilter مربوطه)
public partial class ManageDeprecationJsonApiController : AppController
{
    private readonly IPackageDeprecationManagementService _deprecationManagementService;

    public ManageDeprecationJsonApiController(
        IPackageDeprecationManagementService deprecationManagementService)
    {
        _deprecationManagementService = deprecationManagementService 
            ?? throw new ArgumentNullException(nameof(deprecationManagementService));
    }

    [HttpGet]
    public virtual IActionResult GetAlternatePackageVersions(string id)
    {
        var versions = _deprecationManagementService.GetPossibleAlternatePackageVersions(id);
        // سازگار با خروجی JSON در Core همراه با وضعیت HTTP
        return StatusCode((int)HttpStatusCode.OK, versions);
    }

    [HttpPost]
    // [RequiresAccountConfirmation("deprecate a package")] // در صورت عدم انطباق با Core، به صورت TypeFilter یا Policy پیاده‌سازی شود
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> Deprecate(DeprecatePackageRequest request)
    {
        var isDeprecated = request.IsLegacy || request.HasCriticalBugs || request.IsOther;

        var error = await _deprecationManagementService.UpdateDeprecation(
            GetCurrentUser(),
            request.Id,
            request.Versions.ToList(),
            isDeprecated ? PackageDeprecatedVia.Web : PackageUndeprecatedVia.Web,
            request.IsLegacy,
            request.HasCriticalBugs,
            request.IsOther,
            request.AlternatePackageId,
            request.AlternatePackageVersion,
            request.CustomMessage);

        if (error != null)
        {
            return StatusCode((int)error.Status, new { error = error.Message });
        }

        var packagePluralString = request.Versions.Count() > 1 ? "packages have" : "package has";
        var deprecatedString = isDeprecated ? "deprecated" : "undeprecated";
        TempData["Message"] = 
            $"Your {packagePluralString} been {deprecatedString}. " +
            "It may take several hours for this change to propagate through our system.";

        return StatusCode((int)HttpStatusCode.OK);
    }
}
