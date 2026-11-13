// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace NuGet.Services.Middleware;

public class ForceSslMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _sslPort;
    private readonly HashSet<string> _exclusionPathPatterns;

    public ForceSslMiddleware(RequestDelegate next, int sslPort)
        : this(next, sslPort, Enumerable.Empty<string>())
    {
    }

    public ForceSslMiddleware(
        RequestDelegate next,
        int sslPort,
        IEnumerable<string> exclusionPathPatterns)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _sslPort = sslPort;
        _exclusionPathPatterns = new HashSet<string>(
            exclusionPathPatterns ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool shouldPassThrough = context.Request.IsHttps
            || (context.Request.Path.HasValue && IsExcludedPath(context.Request.Path.Value));

        if (shouldPassThrough)
        {
            await _next(context);
        }
        else
        {
            if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            {
                var request = context.Request;
                var uriBuilder = new UriBuilder
                {
                    Scheme = Uri.UriSchemeHttps,
                    Host = request.Host.Host,
                    Port = _sslPort > 0 ? _sslPort : (request.Host.Port ?? 443),
                    Path = request.PathBase.Add(request.Path).Value,
                    Query = request.QueryString.Value
                };

                context.Response.Redirect(uriBuilder.Uri.AbsoluteUri, permanent: false);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                // ReasonPhrase در HTTP/2 و HTTP/3 پشتیبانی نمی‌شود، اما با نوشتن پیام در بدنه می‌توان نتیجه یکسان گرفت:
                await context.Response.WriteAsync("SSL Required");
            }
        }
    }

    private bool IsExcludedPath(string path)
    {
        return _exclusionPathPatterns.Contains(path);
    }
}
