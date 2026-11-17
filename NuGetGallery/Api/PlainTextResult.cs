// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace NuGetGallery.WebApi;

/// <summary>
/// ایجاد پاسخ متنی ساده برای ASP.NET Core.
/// </summary>
public sealed class PlainTextResult : ContentResult
{
    /// <summary>
    /// ایجاد پاسخ متنی با کد وضعیت 200.
    /// </summary>
    /// <param name="content">محتوای پاسخ.</param>
    public PlainTextResult(string content)
        : this(content, StatusCodes.Status200OK)
    {
    }

    /// <summary>
    /// ایجاد پاسخ متنی با کد وضعیت مشخص.
    /// </summary>
    /// <param name="content">محتوای پاسخ.</param>
    /// <param name="statusCode">کد وضعیت HTTP.</param>
    public PlainTextResult(string content, int statusCode)
    {
        Content = content;
        ContentType = "text/plain; charset=utf-8";
        StatusCode = statusCode;
    }
}
