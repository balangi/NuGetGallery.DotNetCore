# Converted NuGet Gallery Razor views (ASP.NET Core)

This batch contains the six requested `.cshtml` partial views, updated for the ASP.NET Core Razor conventions and intended for a .NET 8/9 application. Add the relevant `@model` namespaces/usings (often through `_ViewImports.cshtml`) and register MVC tag helpers (`@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`).

## External application-specific helpers and integration notes

These original views depend on NuGet Gallery/legacy extensions that are not part of ASP.NET Core. Their application equivalents must be registered in the host project; where a stable direct replacement was possible, the view uses Core markup instead:

- `Url.ManageMyOrganization`, `Url.AccountSettings`, `Url.Package`, `Url.ManagePackage`, `Url.User`, `Url.Search`, and `RouteName.DisplayPackage` are Gallery URL/routing extensions/constants. Port these as Core link-generation helpers or replace with the application's named routes. The confirmation form itself now uses `asp-controller`/`asp-action` and preserves POST plus `@Html.AntiForgeryToken()`.
- `PackageHelper.ShouldRenderUrl`, `AnySafe()`, `ToNuGetNumberString()`, and `ToNuGetShortDateString()` are application extensions and need their Core-compatible implementations/usings.
- `Strings` remains the application's localized resource class.
- `_SupportedFrameworksBadges` is still a project partial and is invoked with the Core `<partial>` tag helper. Ensure its path and model type resolve in the new app; `itemIndex` and `eventName` are passed in its ViewData.
- Legacy `ViewHelpers.PackageImageFallback()` was omitted rather than guessing a Gallery-specific fallback path. Add the Core application's corresponding `onerror` behavior or provide a fallback URL if broken-icon fallback is required. The original class and image alt/ARIA behavior are retained.
- `ViewHelpers.PackagesBreadcrumb` and `ViewHelpers.Breadcrumb` have been replaced by explicit accessible breadcrumb markup. Confirm the destination URL, layout/classes, and breadcrumb semantics against the destination site's navigation conventions; the old helper's exact generated markup could not be reconstructed from the partial alone.
- `Html.BreakWord` is replaced with normal encoded Razor output (`@Model.Id`); apply the site's CSS wrapping rule (for example `overflow-wrap`) to `.package-title` if the old helper inserted break opportunities.
- `Html.HasErrorFor`, `ShowLabelFor`, `ShowEnumDropDownListFor`, `ShowValidationMessagesFor`, `ShowTextBoxFor`, and `ShowCheckboxFor` are replaced with Core `asp-for`, `asp-items`, and `asp-validation-for` controls plus equivalent wrapper classes. Confirm `ReasonChoices` is compatible with `IEnumerable<SelectListItem>` and adjust the validation CSS class mapping to match the former helpers. Core checkbox tag helpers generate the standard hidden false-value field.
- `@Scripts.Render` was replaced with a regular script element. Ensure static-file serving/bundling maps `~/Scripts/gallery/common-multi-select-dropdown.min.js` to the deployed asset path.

The Knockout templates/bindings, visible copy, original classes/IDs, and form semantics are retained. Some framework-specific helper output cannot be preserved byte-for-byte without the original helper implementations.
