# NuGet account Razor view conversion (ASP.NET Core .NET 8/9)

This batch contains six converted Razor view bodies. They retain the supplied labels, messages, IDs, form actions, antiforgery tokens, certificate-table/Knockout markup, account/organization branches, and the original application-specific URL extension calls wherever present.

## Integration requirements and known limits

- These are view fragments that use `@model AccountViewModel`; they expect the application's `AccountViewModel`, `OrganizationAccountViewModel`, nested `ChangeEmail` and `ChangeNotifications` models, and all original properties to be available to the view project.
- Add the ASP.NET Core MVC Tag Helpers to the project's `_ViewImports.cshtml`: `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`. The email and notification property controls use `asp-for` / `asp-validation-for`; model validation and client validation scripts remain an application concern.
- `Url.AccountSettings()`, `Url.ManageMyOrganizations()`, `Url.OrganizationConfirmationRequired(...)`, `Url.ConfirmationRequired()`, and `Url.Action(...)` require the project's route/URL helpers and matching routes/controllers. No replacement routes have been invented. The forms post to the supplied named actions and organization/user controllers; certificate forms retain the original no-explicit-action behavior.
- The original `ViewHelpers.Section` implementation and its CSS/JavaScript contract are not present in these source files. Each section is therefore represented with a consistent inline `<section>` shell and Bootstrap 3 `collapse` toggle, retaining original section IDs and expanded/disabled intent. This is not asserted to be byte-for-byte or behaviorally identical to the unknown helper. The project must load compatible Bootstrap 3 collapse JS/CSS and any account-section styles/JS. Certificates section starts collapsed (`expanded: false` in original); its inner certificate listing and upload block retain their original expanded state. Change-email, notifications, and profile-picture retain `disabled: !Model.CanManage` intent; theme has no original disabled condition.
- `ViewHelpers.GravatarImage(Url, email, accountName, size)` remains an explicit unported dependency in `_AccountProfilePicture.cshtml`; register/provide the original helper for compilation and rendering. It has deliberately not been replaced with a guessed avatar implementation.
- Original `Html.ShowLabelFor`, `ShowPasswordFor`, `ShowTextBoxFor`, `ShowCheckboxFor`, and `ShowValidationMessagesFor` markup/CSS are unknown and were mapped to standard Core Tag Helpers/HTML controls. `Html.HasErrorFor` previously supplied field CSS classes; these cannot be reproduced exactly without that extension's contract. Review styles and validation behavior in the host application. Antiforgery tokens are retained.
- `ViewHelpers.AlertWarning`/`AlertInfo` are rendered as Bootstrap alert divs. Theme selection markup and certificate Knockout bindings are retained; the application must provide the existing client-side view model and its event handlers.
- Build integration and runtime rendering were not run here: the application models, route extensions, helpers, styles, scripts, and project are not included. Full visual/JS parity requires the original helper implementations and site assets.

## Files

- `_AccountCertificates.cshtml`
- `_AccountChangeEmail.cshtml`
- `_AccountChangeNotifications.cshtml`
- `_AccountChangeTheme.cshtml`
- `_AccountConfirmationNotices.cshtml`
- `_AccountProfilePicture.cshtml`
