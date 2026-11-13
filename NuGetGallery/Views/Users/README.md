# NuGet Gallery Razor views — ASP.NET Core 8/9 conversion

This batch contains the seven source views supplied in the request, plus this README and a source-count notice. The prompt says eight views but names only seven; no eighth view was provided. The source attachments available during conversion were inspected and did not include these view texts. Consequently the archive has nine non-empty files, but only seven are converted Razor views; `SOURCE_VIEW_COUNT_NOTICE.md` explicitly records this discrepancy.

## Files

- `Account.cshtml`
- `_DeleteUserAccountForm.cshtml`
- `_UserAccountChangeExternalCredential.cshtml`
- `_UserAccountChangePassword.cshtml`
- `_UserAccountLinkExternalCredential.cshtml`
- `_UserOrganizationsListForDeletedAccount.cshtml`
- `_UserPackagesListForDeletedAccount.cshtml`
- `README.md`
- `SOURCE_VIEW_COUNT_NOTICE.md`

## Conversion notes / integration requirements

1. These are Razor view snippets, not a standalone application. Place them in the corresponding ASP.NET Core view folders, provide the referenced view models and domain services, and configure `_ViewImports.cshtml` for model namespaces and MVC tag helpers (`@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`). Legacy gallery types are assumed available through project/global imports; remove any obsolete `NuGetGallery.Areas.Admin.ViewModels` import and use the actual Core model namespace for `DeleteAccountAsAdminViewModel`.
2. `asp-controller` / `asp-action` form helpers generate standard Core forms and antiforgery tokens. The delete form also submits `_method=DELETE`; ASP.NET Core does **not** treat that field as method override by itself. Configure and test an appropriate method-override middleware/endpoint convention if the endpoint truly requires HTTP DELETE. Otherwise remove `_method` and accept POST on the action. Do not rely on method override without backend configuration.
3. The delete form retains visible and hidden controls from the old page. Since same-name inputs can bind with competing values, review `Signature` and `ShouldUnlist` behavior in the target model binder/action, especially the hidden checkbox value.
4. Legacy `ViewHelpers.Section` became native `<details>/<summary>` sections, requiring no helper or JavaScript. `ViewHelpers.SectionsScript` is intentionally omitted; if the Gallery's previous section expand/collapse behavior or state persistence is required, implement it as a small project-owned script.
5. The profile breadcrumb in `Account.cshtml` is ordinary accessible breadcrumb markup, a minimal substitute for `ViewHelpers.BreadcrumbWithProfile`. Adapt its user URL and CSS to the host Gallery layout. `ViewHelpers.GetColumnClasses` is retained as a project-specific helper; replace with static Bootstrap grid classes if that helper is unavailable.
6. `@Html.Partial` calls have been migrated to `<partial name="..." model="Model" />`. The other referenced partials (`_AccountConfirmationNotices`, `_AccountChangeEmail`, `_AccountChangeNotifications`, `_AccountProfilePicture`, `_AccountCertificates`, `_AccountChangeTheme`) are dependencies and were not supplied or converted here.
7. Forms use `asp-for`, `asp-validation-for`, `asp-validation-summary`, standard password/text/checkbox inputs and Bootstrap-style classes. Include client validation scripts if unobtrusive validation is required. Keep server-side validation and antiforgery validation enabled.
8. Legacy URL extensions are retained where provided by the Gallery (`Url.Package`, `Url.User`, `Url.ManageMyOrganization`, `Url.ManagePackage`, `Url.ConfirmationRequired`, `Url.AddUserCertificate`, `Url.GetUserCertificates`). Implement equivalent ASP.NET Core `IUrlHelper` extensions/routing if not already ported. `GravatarHelper.Url`, `PackageHelper.ShouldRenderUrl`, and `ToNuGetNumberString()` are also Gallery-specific backend dependencies; port them or replace with local equivalents.
9. Package owner links use `Url.User(owner.Username)` and a simple icon instead of `ViewHelpers.UserLinkWithIcon`. The package icon uses an explicit `Url.Content` fallback path and `onerror`; verify the fallback asset location and CSP policy in the consuming site. Adjust `owner.Username` if the Core owner model exposes a different name property. Package name/version truncation replaces `BreakWord`/`Abbreviate`; styling can provide word breaking.
10. Certificate JS uses `@Url.Content("~/Scripts/gallery/certificates.min.js")` and an inline script with `@Html.GetCSPNonce()`. `GetCSPNonce()` is a project-specific HTML helper that must be ported; otherwise use the application's CSP nonce mechanism. The inline validation-errors template replaces `AlertDanger` with Bootstrap alert markup. Confirm that the site's nonce/CSP handling permits this template script.
11. The exact output depends on the host site's Bootstrap version and icon font. The organization/package tables include responsive utility classes; map the `d-none d-sm-table-cell` classes to the site's Bootstrap generation if necessary.

## Important verification

This artifact is a source-level migration, not a compiled project. Build the consuming application and verify model-property names, route extensions, form method semantics, CSP nonce helper, package fallback asset, and partial availability against the actual ASP.NET Core 8/9 Gallery codebase. The only missing source-level input is the eighth view mentioned but not named/provided in the request.
