using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;

namespace NuGetGallery;

public static class QuietLog
{
    private static ITelemetryClient Telemetry;

    public static void UseTelemetryClient(ITelemetryClient telemetryClient)
    {
        Telemetry = telemetryClient
            ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    public static void LogHandledException(Exception e)
    {
        var aggregateExceptionId = Guid.NewGuid().ToString();

        if (e is AggregateException aggregateException)
        {
            LogHandledExceptionCore(aggregateException, aggregateExceptionId);

            foreach (var innerException in aggregateException.InnerExceptions)
            {
                LogHandledExceptionCore(innerException, aggregateExceptionId);
            }
        }
        else
        {
            LogHandledExceptionCore(e, aggregateExceptionId);

            if (e.InnerException != null)
            {
                LogHandledExceptionCore(e.InnerException, aggregateExceptionId);
            }
        }
    }

    private static void LogHandledExceptionCore(
        Exception e,
        string aggregateExceptionId)
    {
        try
        {
            Telemetry?.TrackException(
                e,
                new Dictionary<string, string>
                {
                    { "aggregateExceptionId", aggregateExceptionId }
                });
        }
        catch
        {
            // Logging must not cause another exception.
        }
    }

    internal static bool IsPIIRoute(
        RouteData route,
        out string operation)
    {
        if (route == null)
        {
            operation = string.Empty;
            return false;
        }

        var controller = route.Values["controller"]?.ToString();
        var action = route.Values["action"]?.ToString();

        operation = $"{controller}/{action}";

        return Obfuscator.ObfuscatedActions.Contains(operation);
    }

    /// <summary>
    /// Gets obfuscated server variables for PII routes.
    /// </summary>
    internal static Dictionary<string, string> GetObfuscatedServerVariables(
        HttpContext currentContext)
    {
        if (currentContext == null)
        {
            return null;
        }

        var routeData = currentContext.GetRouteData();

        if (!IsPIIRoute(routeData, out var operation))
        {
            return null;
        }

        var requestUrl = currentContext.Request.GetDisplayUrl();

        if (!Uri.TryCreate(
                requestUrl,
                UriKind.Absolute,
                out var requestUri))
        {
            return null;
        }

        var obfuscatedUrl = Obfuscator.DefaultObfuscatedUrl(requestUri);

        return new Dictionary<string, string>
        {
            { "HTTP_REFERER", obfuscatedUrl },
            { "PATH_INFO", operation },
            { "PATH_TRANSLATED", operation },
            { "SCRIPT_NAME", operation },
            { "URL", obfuscatedUrl }
        };
    }
}
