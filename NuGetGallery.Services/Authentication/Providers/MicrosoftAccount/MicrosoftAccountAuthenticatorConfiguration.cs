// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using System.Globalization;

namespace NuGetGallery.Authentication.Providers.MicrosoftAccount;

public class MicrosoftAccountAuthenticatorConfiguration : AuthenticatorConfiguration
{
    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public MicrosoftAccountAuthenticatorConfiguration()
    {
        AuthenticationType = MicrosoftAccountAuthenticator.DefaultAuthenticationType;
    }

    public override void ApplyToAuthSecurityOptions(AuthenticationSchemeOptions options)
    {
        base.ApplyToAuthSecurityOptions(options);

        if (options is not MicrosoftAccountOptions microsoftAccountOptions)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                ServicesStrings.MissingRequiredConfigurationValue,
                "Auth.MicrosoftAccount.ClientId"));
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                ServicesStrings.MissingRequiredConfigurationValue,
                "Auth.MicrosoftAccount.ClientSecret"));
        }

        microsoftAccountOptions.ClientId = ClientId;
        microsoftAccountOptions.ClientSecret = ClientSecret;
    }
}
