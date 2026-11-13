# NuGet staging views — ASP.NET Core 8/9 conversion

This batch contains converted Razor view files for `CreateGroup`, `DeleteGroup`, `Group`, and `MovePackage`. They are view conversions only and remain subject to application integration; they are not a complete, independently buildable application. No build was run.

## Integration dependencies and checks

- Import `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` in `_ViewImports.cshtml` (alongside the application's model/namespace imports) so `asp-for`, `asp-items`, validation, and partial Tag Helpers are available.
- Supply the matching backend view models and enum types, controller actions/routes, validation metadata, and service behavior. The actions used in `Url.Action` must support the indicated POST parameters and routes. Form POSTs retain antiforgery tokens.
- The original custom URL extensions are intentionally preserved rather than guessed: `Url.ManageMyStagingGroups`, `Url.ManageStagingGroup`, `Url.ManageMyPackages`, `Url.PromoteStagingGroup`, `Url.ResendStagingGroup`, `Url.DeleteStagingGroup`, package download/delete/replace/promote/resend URL helpers, `Url.UpdateStagedPackageListed`, and `Url.Absolute` all require equivalent Core extensions/implementations.
- `ViewHelpers.GetColumnClasses(ViewBag)` and `Html.GetCSPNonce()` are preserved dependencies; port and register their Core equivalents. The nonce provider must be consistent with the response Content-Security-Policy.
- `Group.cshtml` references `~/Scripts/gallery/page-staging-group.min.js`. The replacement emits a standard script URL through `Url.Content`; ensure this file is deployed under static web root and served by static-file middleware, or adapt the URL to the application's asset pipeline/bundle system. This conversion does not create or bundle the JavaScript asset.
- The JSON payload uses `System.Text.Json.JsonSerializer.Serialize` with the default safe encoder and PascalCase anonymous-object properties, emitted using `Html.Raw` for Knockout consumption. Do not replace the encoder with `UnsafeRelaxedJsonEscaping`; verify serialization and client compatibility with actual data.
- `_ValidationIssue.cshtml` must be available at `~/Views/Packages/_ValidationIssue.cshtml`. Bootstrap 3 styles/scripts and the legacy `alert-brand-warning`, `alert-brand-info`, and `alert-brand-danger` styling, icons, and other site CSS must also be present.
- The Owner and GroupId dropdowns are built using `SelectList`. Verify source collection types and option values against the real backend models.

## Conversion notes

- MVC5 `Html.BeginForm` calls were converted to explicit POST forms. The staging controller forms use `Url.Action` with the source action/controller and route values (no custom route helper was inferred); all source antiforgery tokens are retained.
- Legacy label/input/validation helpers became built-in Tag Helpers or explicit markup. ModelState errors drive the form-level `has-error` class where applicable; Delete and Move validation summaries are ModelOnly.
- `Html.Partial` became a partial Tag Helper. Legacy alert helper invocations became explicit Bootstrap 3 brand alert elements with the original alert message text.
- The source's custom URL helpers and `ViewHelpers.GetColumnClasses(ViewBag)` remain as application-specific dependencies. Date formatting/status logic, labels, table branches, Knockout markup/bindings, IDs, accessibility attributes, and front-end behavior were retained.
