// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace NuGetGallery;

public interface IFileStorageService : ICoreFileStorageService
{
    Task<IActionResult> CreateDownloadFileActionResultAsync(
        Uri requestUrl,
        string folderName,
        string fileName,
        string versionParameter);

    Task<bool> IsAvailableAsync();
}
