// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        // ۱. حل خطای long به int: کست ایمن با در نظر گرفتن حد سقف int.MaxValue
        var contentLengthLong = context.Request.ContentLength ?? 0;
        var safeContentLength = (int)Math.Min(contentLengthLong, int.MaxValue);

        var progress = new AsyncFileUploadProgress(safeContentLength);
        _cacheService.SetProgress(uploadKey, progress);

        // فعال‌سازی Buffering تا بدنه برای مراحل و اکشن‌های بعدی بازخوانی‌پذیر باشد
        context.Request.EnableBuffering();

        // ۲. حل خطای string به Encoding: استخراج انکودینگ از هدر یا استفاده از UTF8 به عنوان پیش‌فرض
        var requestEncoding = GetRequestEncoding(context.Request.ContentType) ?? Encoding.UTF8;
        var requestParser = new AsyncFileUploadRequestParser(boundary, requestEncoding);

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
            // برگرداندن موقعیت استریم به ابتدا جهت خواندن توسط فرم‌ریدر یا کنترولر
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

        // ۳. حل خطای IHeaderDictionary به NameValueCollection
        var headersCollection = ToNameValueCollection(request.Headers);
        var uploadTracingKey = UploadHelper.GetUploadTracingKey(headersCollection);
        uploadKey = username + uploadTracingKey;

        return true;
    }

    private static NameValueCollection ToNameValueCollection(IHeaderDictionary headers)
    {
        var collection = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
        if (headers == null)
        {
            return collection;
        }

        foreach (var header in headers)
        {
            collection.Add(header.Key, header.Value.ToString());
        }

        return collection;
    }

    private static Encoding GetRequestEncoding(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return null;
        }

        try
        {
            var mediaType = Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            return mediaType.Encoding;
        }
        catch
        {
            return null;
        }
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

        if (progress.TotalBytesRead < progress.ContentLength)
        {
            progress.TotalBytesRead = progress.ContentLength;
            reportProgress();
        }
    }
}
