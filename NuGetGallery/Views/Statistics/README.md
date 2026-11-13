# Converted NuGet statistics Razor views (ASP.NET Core 8/9)

This folder contains the seven converted `.cshtml` views. The views retain the provided markup, Knockout templates/data-bindings, report tables, chart SVGs, labels and custom URL-helper calls, with the requested MVC-to-Core partial/script substitutions.

## Integration notes

- Add the project namespaces containing `StatisticsPackagesViewModel` and `StatisticsWeeklyUsageItem` to `_ViewImports.cshtml` with `@using Your.Project.Namespace` (substitute the actual namespaces). The page views declare `@model StatisticsPackagesViewModel`; make that model visible to the views through this import or fully qualify it.
- Enable Core MVC Razor tag helpers in `_ViewImports.cshtml` using `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`. This is needed for `<partial name="..." model="Model" />`.
- Port/register the custom `Url.Package(...)` and `Url.Statistics...(...)` extension methods for ASP.NET Core's `IUrlHelper`; these are application-specific route helpers and are intentionally retained in the views.
- `@Html.GetCSPNonce()` is an application-specific HTML helper used for the inline scripts. Port/register it for ASP.NET Core and ensure the nonce it emits matches the Content Security Policy response header. It is present on the first report's inline script and on the formerly unprotected inline scripts in PackageDownloadsDetail, Packages and PackageVersions.
- `@Url.Content("~/Scripts/gallery/stats.min.js")` expects the asset at `wwwroot/Scripts/gallery/stats.min.js` when using the default static-file pipeline; otherwise adapt the path to the project's asset pipeline. The file must be deployed and served.
- The host layout/page must load jQuery, Knockout, and moment before the relevant inline/report scripts run. The views also rely on the existing statistics client script and Bootstrap-compatible CSS/classes (including collapse behavior).
- Supply the underlying view model/data members and the report endpoints expected by the retained `renderGraph(...)` calls.

No build or runtime verification is claimed; this output was checked for expected view basenames, required preserved structures/helpers and ZIP integrity only.
