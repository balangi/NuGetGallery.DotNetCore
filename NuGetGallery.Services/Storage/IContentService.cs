// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Html;

namespace NuGetGallery;

public interface IContentService
{
    Task<IHtmlContent> GetContentItemAsync(string name, TimeSpan expiresIn);
    void ClearCache();
}
