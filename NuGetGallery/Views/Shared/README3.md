# NuGet Gallery Razor views — ASP.NET Core conversion (batch 9)

This archive contains the six requested converted Razor views:

- `_SearchBar.cshtml`
- `Confirm.cshtml`
- `ConfirmationRequired.cshtml`
- `ListPackages.cshtml`
- `SiteMenu.cshtml`
- `Title.cshtml`

They are adapted from the supplied ASP.NET MVC 5 / .NET Framework 4.6-era views for an ASP.NET Core MVC Razor application. The conversion keeps the original page copy, form semantics, CSS hooks, IDs, accessibility attributes, conditional behavior, package search filters and analytics events wherever feasible. This is a view-layer conversion, not a standalone application: the NuGet Gallery models, services, helpers, resources, routes, partials and assets are still required.

## Integration

1. Place each file in its intended Core view/partial location. Keep partial names and paths resolvable from the calling view or use explicit relative paths appropriate to the destination project.
2. Ensure `_ViewImports.cshtml` (or the relevant view scope) imports the namespaces containing the view models, `GalleryConstants`, `Config`, `ViewHelpers`, `StatisticsHelper`, `AdminHelper`, `CoreConstants`, and any extension methods used below.
3. Register the MVC tag helpers, for example:
   ```cshtml
   @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
   ```
   This is needed for the `<partial>` elements and `asp-controller` / `asp-action` links.
4. The layout must render the `bottomScripts` section used by `ListPackages.cshtml` (typically `@await RenderSectionAsync("bottomScripts", required: false)`). Configure the destination app's CSP nonce helper/policy and static-file mapping for the package-list script.
5. Confirm the relevant controller actions, routes, authorization and authentication configuration in the target ASP.NET Core app. Compile these views against the actual application; this archive cannot provide the host project's application-specific types.

## Core-specific changes made

- Legacy `Html.Partial(...)` calls are expressed with the Core `<partial>` tag helper, including the per-package `ViewData` values needed by `_ListPackage`.
- The menu's legacy `Request.IsAuthenticated` check is replaced by the Core identity check (`User.Identity?.IsAuthenticated ?? false`).
- Legacy link helpers in the confirmation view are changed to Core anchor tag-helper links. The menu continues to use Gallery URL extensions because their route behavior is application-specific.
- The bundled `Scripts.RenderFormat(...)` call is replaced by a normal script element with `asp-append-version="true"` and a CSP nonce. Make sure static web assets/bundling expose the referenced path.
- JavaScript values that are JSON serialized use `System.Text.Json`; ensure the target framework includes this namespace and retain safe JSON serialization if adapting the inline event code.
- The search input's values and submitted query remain bound through the existing view data and standard GET form behavior.

## Application-specific dependencies and checks

These names are not built into ASP.NET Core and must exist in the destination NuGet Gallery application (or be replaced with its equivalents):

- **Types/models:** `ConfirmationViewModel`, `PackageListViewModel`, package/framework filter models, `GalleryConstants`, `Config`, `CoreConstants`, and `NuGetGallery.Strings`.
- **Helpers/extensions:** `ViewHelpers.GetColumnClasses`, `ViewHelpers.AlertDanger`, `ViewHelpers.AlertInfo`, `ViewHelpers.PreviousNextPager`, `ToNuGetNumberString`, `BreakWord`, `Html.GetCSPNonce`, plus `Url.Home`, `Url.PackageList`, `Url.UploadPackage`, `Url.Statistics`, `Url.Admin`, and `Url.Downloads`.
- **Services/resources:** `StatisticsHelper`, `AdminHelper`, and any localized resource classes.
- **Other partials:** `_ConfirmationResendForm.cshtml`, `_AutocompleteTemplate.cshtml`, and `_ListPackage.cshtml` must be available and themselves Core-compatible. `_ConfirmationResendForm` should preserve the destination app's POST and antiforgery behavior.
- **Routes/actions:** The confirmation links target `Users/Account`; verify these controller/action names and route conventions, or change their `asp-controller`/`asp-action` values. Verify the menu's Gallery URL methods map to the same destination routes.
- **Assets/client code:** jQuery, `window.nuget` analytics methods, search filter scripts, and `~/Scripts/gallery/page-list-packages.min.js` must be available. Retest search filter query names and event payloads against the target JavaScript.
- **Formatting/security:** Review the app's CSP nonce implementation. JSON serialization is used for selected event payload values; any other values interpolated into inline JavaScript (including model-derived framework strings and generated IDs) should be reviewed and encoded safely according to the host application's CSP/XSS conventions.

`AlertDanger` and `AlertInfo` are legacy Gallery helper calls that render templated content. If their signatures are not available in the Core project, replace them with Core-compatible partial/tag-helper components while preserving alert roles and the conditional message content. Likewise, `BreakWord` and the pager helper require a compatible implementation or an explicit replacement. Exact output of those proprietary helpers cannot be reconstructed from the view source alone.

## Important compatibility notes

- The original `@helper` declarations from `ListPackages.cshtml` are inlined into the corresponding Core markup because Razor `@helper` is not supported by ASP.NET Core Razor. Keep the inlined package/framework radio and framework-group behavior synchronized if changing filters.
- The views retain Gallery-specific HTML and CSS class names. Their appearance depends on the site's existing CSS and client-side scripts.
- Review the target app's layout conventions, view locations, tag-helper imports, route casing, output encoding, nullable settings, and model namespaces before deployment. Build and test success depends on those destination-project components.

## Files

The archive includes exactly the six `.cshtml` views above plus this README. No project binaries or application source code are included.
