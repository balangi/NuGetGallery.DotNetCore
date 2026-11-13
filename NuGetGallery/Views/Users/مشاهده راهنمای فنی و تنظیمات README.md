# NuGet Gallery Razor Views (ASP.NET Core)

This folder contains six converted `.cshtml` views intended for integration into the NuGet Gallery ASP.NET Core application. They are view files, not a standalone runnable application; the Gallery's view models, shared layout, services, routes, helpers, static assets, and client-side bundles must be supplied by the host project.

## Views and adaptations

- **`Profiles.cshtml`** — profile page based on `UserProfileModel`. The MVC 5 `@helper` for the delete-account link was adapted to a Razor `@functions` method returning `IHtmlContent`, using `TagBuilder` and encoded attributes/content. Uses Gallery profile/paging helpers and the package-list client script.
- **`ResetPassword.cshtml`** — password-reset form based on `PasswordResetViewModel`; retains validation and authentication layout metadata. Uses ASP.NET Core MVC form/validation helpers and Gallery alert/layout helpers.
- **`Thanks.cshtml`** — registration confirmation page; uses the signed-in user state and Gallery route extensions for confirmation, settings, and package upload.
- **`TransformToOrganization.cshtml`** — account transformation form based on `TransformAccountViewModel`; keeps the warning/confirmation flow and client-side submit confirmation. Its script uses jQuery, the validation plugin, and `window.nuget.confirmEvent` / `window.nuget.formatString`.
- **`TransformToOrganizationFailed.cshtml`** — transformation error page based on `TransformAccountFailedViewModel`.
- **`TrustedPublishing.cshtml`** — trusted publisher policy management based on `TrustedPublisherPolicyListViewModel`; includes the Knockout-driven policy UI, anti-forgery token, JSON bootstrap data, CSP nonce, and client-side policy actions.

The conversion uses ASP.NET Core Razor conventions where needed (including `@functions`, `IHtmlContent`/`TagBuilder`, `asp-append-version`, ASP.NET Core request access, and Core-compatible helper patterns). Application-specific `ViewBag`, `ViewHelpers`, `Url` extensions, and the `CurrentUser` context remain intentional Gallery integration points: provide their ASP.NET Core equivalents in the host application. Razor directives such as `@model` and the view names are preserved for application integration.

## Knockout templates and scripts

`TrustedPublishing.cshtml` contains the named inline templates `error-container`, `manage-policies`, `policy-details`, and `upsert-policy`, plus Knockout bindings and conditional `ko` comment blocks. Its bindings consume the initial policy/owner/scope data serialized by the view and are implemented by:

- `~/Scripts/gallery/page-trusted-publishing.min.js`

`Profiles.cshtml` references:

- `~/Scripts/gallery/page-list-packages.min.js`

These paths are resolved by the host application's static-file setup and must point to compatible Gallery bundles. The transform form additionally expects the site's jQuery, jQuery Validation, and NuGet Gallery JavaScript globals; it uses an inline script in the `bottomScripts` section.

## Host application dependencies / integration

- ASP.NET Core MVC with Razor Views, a compatible shared layout, and the standard MVC tag-helper imports/configuration. The layout must render the script sections used by the views (`BottomScripts` and/or `bottomScripts`; Razor section names are case-sensitive, so align the view declarations and layout or normalize them consistently).
- NuGet Gallery view-model and service types referenced by the `@model` declarations and expressions, including profile, password reset, account transformation, and trusted publisher policy types.
- Gallery-specific `ViewHelpers`, URL/route extensions, `CurrentUser`, `GalleryConstants`, formatting extensions, authentication/authorization, anti-forgery, and validation infrastructure. These are not defined in this package of views.
- The Gallery frontend assets and compatible jQuery, jQuery Validation, Knockout, and Gallery `window.nuget` scripts where used. Trusted Publishing also uses Newtonsoft.Json and Gallery JSON/CSP helper extensions as configured by the host.
- Matching routes/endpoints for the URL helpers and APIs called by the trusted-publishing client code, plus the referenced content/image assets.

## Integration notes

1. Copy the views into the corresponding feature folders in the host project's `Views` tree and ensure model namespaces and shared imports resolve.
2. Confirm that the host layout renders the required script sections and that script/static asset paths correspond to the deployed bundles.
3. Verify the Gallery-specific helpers, URL methods, model validation, CSP nonce, anti-forgery behavior, Knockout view model, and API endpoints in the target application. This view-only conversion does not migrate controllers, models, helpers, scripts, or backend behavior.
4. Build and exercise the pages in the target ASP.NET Core application; a successful file conversion alone does not guarantee runtime compatibility with a particular Gallery version.
