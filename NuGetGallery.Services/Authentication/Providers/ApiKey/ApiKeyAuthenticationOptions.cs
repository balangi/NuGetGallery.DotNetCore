// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;

namespace NuGetGallery.Authentication.Providers.ApiKey;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";

    public string ApiKeyHeaderName { get; set; }
    public string ApiKeyClaim { get; set; }

    public ApiKeyAuthenticationOptions()
    {
        // مقداردهی پیش‌فرض سازگار با ASP.NET Core
        ForwardAuthenticate = null;
        ApiKeyHeaderName = ServicesConstants.ApiKeyHeaderName;
        ApiKeyClaim = NuGetClaims.ApiKey;
    }
}
