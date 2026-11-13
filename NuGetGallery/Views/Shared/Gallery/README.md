# Converted NuGet Gallery Razor views (ASP.NET Core)

Files: `Footer.cshtml`, `Header.cshtml`, and `Layout.cshtml` are converted view source based on the supplied Framework 4.6 originals. Place the partials at `Views/Shared/Gallery/Header.cshtml` and `Views/Shared/Gallery/Footer.cshtml` (the layout uses those absolute view paths); provide `_SearchBar` and `_AutocompleteTemplate` as resolvable partials.

## Required project-specific ports / dependencies

The supplied source did not include implementations of these dependencies. The project must port/register them for Core; no stubs or replacements are supplied here:

- `Config.Current` and its configuration properties (including brand, external branding/status links, password-login flags and site options).
- Gallery URL extensions/helpers: `Url.Contact`, `Home`, `PackageList`, `UploadPackage`, `Statistics`, `Admin`, `Downloads`, `LogOn`, `User`, `AccountSettings`, `ManageMyTrustedPublishing`, `ManageMyApiKeys`, `ManageMyOrganizations`, `ManageMyPackages`, `Current`, `About`, `Terms`, `YourPrivacyChoices`, `Privacy`, `Absolute`, `GetCanonicalLinkUrl`; plus `GalleryConstants` values.
- `ViewHelpers.ImageFallback`, `ViewHelpers.ReleaseTag`, `ViewHelpers.GravatarImage`, `ViewHelpers.AnalyticsScript`, `ReleaseMeta`, `InstrumentationScript`, and `CookieComplianceScript` must be ported to Core-compatible `IHtmlContent`/Razor implementations. Their original fallback, release, analytics, instrumentation, gravatar and cookie behavior cannot be confirmed from the provided files.
- User extensions `HasPasswordLogin`, `HasMultiFactorAuthenticationEnabled`, `WasMicrosoftAccountUsedForSignin`, `HasExternalLogin`, and the project-specific `CurrentUser`, `ShowAuthInHeader`, `LinkOpenSearchXml`, `StatisticsHelper`, `AdminHelper`, and `CoreConstants`.
- MVC route/action setup, antiforgery configuration, static assets, and required Bootstrap 3 scripts/styles.

The footer retains `ImageFallback` and `ReleaseTag` calls rather than silently dropping their output. Footer layout fidelity depends on a correct Core port of `ImageFallback`; release-tag behavior depends on `ReleaseTag`. Layout fidelity is likewise conditional on the original custom helpers/extensions being ported.

## Intentional safety/layout notes

- `GetColumnClasses(ViewBag)` was an unknown custom helper, so the transient alert columns use explicit `col-sm-12`; exact prior alert-column layout cannot be guaranteed.
- `RawErrorMessage` is output with normal Razor encoding (`@rawErrorMessage`) rather than `Html.Raw`. This intentionally changes the legacy raw-HTML behavior to avoid an XSS risk; if rich HTML is truly required, sanitize it with a trusted allowlist before rendering.
- The account sign-out action is a POST form with an antiforgery token; CSS keeps its control visually menu-like without an inline script or fake link.
- ASP.NET Core does not necessarily retain legacy route/URL semantics. Review all custom URL helpers and the absolute partial paths against the target app's view locations.

These views are not asserted to compile as a standalone project: the custom implementations above were not present in the supplied view source and must be ported and validated in the destination application.
