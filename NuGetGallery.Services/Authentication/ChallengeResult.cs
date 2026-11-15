// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;

namespace NuGetGallery;

/// <summary>
/// نگاشت سازگار با نسخه قدیمی جهت ارجاع به ChallengeResult بومی ASP.NET Core
/// </summary>
public class ChallengeResult : Microsoft.AspNetCore.Mvc.ChallengeResult
{
    public ChallengeResult(string provider, string redirectUri, IDictionary<string, string> properties = null)
        : base(provider, CreateProperties(redirectUri, properties))
    {
        LoginProvider = provider;
        RedirectUri = redirectUri;
        Properties = properties ?? new Dictionary<string, string>();
    }

    public string LoginProvider { get; set; }
    public string RedirectUri { get; set; }
    public IDictionary<string, string> Properties { get; set; }

    private static AuthenticationProperties CreateProperties(string redirectUri, IDictionary<string, string> properties)
    {
        var authProperties = new AuthenticationProperties
        {
            RedirectUri = redirectUri
        };

        if (properties != null)
        {
            foreach (var pair in properties)
            {
                authProperties.Items[pair.Key] = pair.Value;
            }
        }

        return authProperties;
    }
}
