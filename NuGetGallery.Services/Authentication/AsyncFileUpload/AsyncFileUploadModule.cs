// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGetGallery.AsyncFileUpload;

[Obsolete(
    "AsyncFileUploadModule is obsolete and does not work on ASP.NET Core. " +
    "Use NuGetGallery.AsyncFileUpload.AsyncFileUploadMiddleware (register with " +
    "app.UseMiddleware<AsyncFileUploadMiddleware>()).",
    error: false)] // فعلاً هشدار؛ بعد از مهاجرت کامل به true تغییر دهید
public sealed class AsyncFileUploadModule : IHttpModule
{
    [Obsolete("Use AsyncFileUploadMiddleware instead.", error: false)]
    public AsyncFileUploadModule()
    {
    }

    [Obsolete("Use AsyncFileUploadMiddleware instead.", error: false)]
    public void Dispose()
    {
    }

    [Obsolete("Use AsyncFileUploadMiddleware instead.", error: false)]
    public void Init(HttpApplication application)
    {
        // ...
    }

    public class HttpApplication
    {
    }
}

public interface IHttpModule
{
}