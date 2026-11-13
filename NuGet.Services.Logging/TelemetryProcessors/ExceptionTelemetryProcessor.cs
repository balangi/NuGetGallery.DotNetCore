// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NuGet.Services.Logging;

/// <summary>
/// An Application Insights telemetry processor for <see cref="ExceptionTelemetry" />.
/// </summary>
public sealed class ExceptionTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _nextProcessor;
    private readonly TelemetryClient _telemetryClient;

    public ExceptionTelemetryProcessor(ITelemetryProcessor nextProcessor, TelemetryClient telemetryClient)
    {
        _nextProcessor = nextProcessor ?? throw new ArgumentNullException(nameof(nextProcessor));
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    public void Process(ITelemetry item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is ExceptionTelemetry exceptionTelemetry)
        {
            // بررسی نوع خطا در دات‌نت کور (معادل HttpException قدیمی)
            int? statusCode = null;

            if (exceptionTelemetry.Exception is BadHttpRequestException badHttpRequestException)
            {
                statusCode = badHttpRequestException.StatusCode;
            }

            if (statusCode.HasValue && statusCode.Value < 500)
            {
                // لاگ کردن خطاهای کلاینت (زیر ۵۰۰) به عنوان هشدار برای جلوگیری از شلوغی تله‌متری اکسپشن‌ها
                var traceTelemetry = Convert(exceptionTelemetry, exceptionTelemetry.Exception);
                _telemetryClient.TrackTrace(traceTelemetry);
                return;
            }
        }

        _nextProcessor.Process(item);
    }

    private static TraceTelemetry Convert(ExceptionTelemetry exceptionTelemetry, Exception exception)
    {
        var traceTelemetry = new TraceTelemetry(exception.Message, SeverityLevel.Warning);

        // ۱. کپی کردن متادیتای Cloud Role
        traceTelemetry.Context.Cloud.RoleInstance = exceptionTelemetry.Context.Cloud.RoleInstance;
        traceTelemetry.Context.Cloud.RoleName = exceptionTelemetry.Context.Cloud.RoleName;

        // ۲. کپی کردن اطلاعات کلاینت/IP
        traceTelemetry.Context.Location.Ip = exceptionTelemetry.Context.Location.Ip;

        // ۳. کپی کردن مشخصات Operation (در صورت نیاز از طریق کانتکست)
        traceTelemetry.Context.Operation.Name = exceptionTelemetry.Context.Operation.Name;

        // ۴. کپی کردن Custom Properties
        foreach (var property in exceptionTelemetry.Properties)
        {
            traceTelemetry.Properties[property.Key] = property.Value;
        }

        traceTelemetry.Properties["exception"] = JObject.FromObject(exception).ToString(Formatting.None);

        // ۵. تنظیم زمان رخداد
        traceTelemetry.Timestamp = exceptionTelemetry.Timestamp;

        return traceTelemetry;
    }
}
