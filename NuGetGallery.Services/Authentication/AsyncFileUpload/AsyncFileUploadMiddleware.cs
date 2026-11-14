// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NuGetGallery.Helpers;

namespace NuGetGallery.AsyncFileUpload;

public class AsyncFileUploadMiddleware
{
    private const int MinContentLengthForProgress = 4096;

    private readonly RequestDelegate _next;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AsyncFileUploadMiddleware> _logger;

    public AsyncFileUploadMiddleware(
        RequestDelegate next,
        ICacheService cacheService,
        ILogger<AsyncFileUploadMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!TryGetUploadContext(context, out var boundary, out var uploadKey))
        {
            await _next(context);
            return;
        }

        var progress = new AsyncFileUploadProgress(context.Request.ContentLength ?? 0);
        _cacheService.SetProgress(uploadKey, progress);

        // Buffering لازم است تا بدنه هم برای پایش و هم برای کنترلر قابل خواندن باشد.
        context.Request.EnableBuffering();

        var requestParser = new AsyncFileUploadRequestParser(boundary, context.Request.ContentType);

        try
        {
            await ReadStreamAsync(
                context.Request.Body,
                context.RequestAborted,
                progress,
                requestParser,
                () => _cacheService.SetProgress(uploadKey, progress));
        }
        finally
        {
            // مهم: بدنه را برای اکشن بعدی به ابتدا برمی‌گردانیم.
            context.Request.Body.Position = 0;
        }

        await _next(context);
    }

    private bool TryGetUploadContext(HttpContext context, out string boundary, out string uploadKey)
    {
        boundary = null;
        uploadKey = null;

        var request = context.Request;

        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        if (context.User?.Identity is not { IsAuthenticated: true } identity)
        {
            return false;
        }

        var contentType = request.ContentType;
        if (string.IsNullOrEmpty(contentType)
            || !contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var boundaryIndex = contentType.IndexOf("boundary=", StringComparison.OrdinalIgnoreCase);
        if (boundaryIndex < 0)
        {
            return false;
        }

        // نکته: نبود Content-Length یعنی chunked یا نامشخص؛ قبلاً هم رد می‌شد.
        if (request.ContentLength is null or < MinContentLengthForProgress)
        {
            return false;
        }

        var username = identity.Name;
        if (string.IsNullOrEmpty(username))
        {
            return false;
        }

        boundary = "--" + contentType.Substring(boundaryIndex + "boundary=".Length).Trim('"');

        var uploadTracingKey = UploadHelper.GetUploadTracingKey(request.Headers);
        uploadKey = username + uploadTracingKey;

        return true;
    }

    private static async Task ReadStreamAsync(
        Stream stream,
        CancellationToken cancellationToken,
        AsyncFileUploadProgress progress,
        AsyncFileUploadRequestParser parser,
        Action reportProgress)
    {
        const int bufferSize = 1024 * 4;

        var buffer = new byte[bufferSize];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken)) > 0)
        {
            progress.TotalBytesRead += bytesRead;
            parser.ParseNext(buffer, bytesRead);
            progress.FileName = parser.CurrentFileName;
            reportProgress();
        }

        // همان رفتار «خواندن صفر = پایان» نسخه قبلی
        if (progress.TotalBytesRead < progress.ContentLength)
        {
            progress.TotalBytesRead = progress.ContentLength;
            reportProgress();
        }
    }
}
