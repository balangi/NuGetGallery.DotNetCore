// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NuGetGallery.Diagnostics;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class SendErrorsToTelemetryAttribute :
    Attribute, IExceptionFilter
{
    private readonly TelemetryClient _telemetryClient;

    public SendErrorsToTelemetryAttribute()
    {
        // در ASP.NET Core بهتر است TelemetryClient از طریق DI تزریق شود، 
        // اما برای سازگاری با ساختار Attribute می‌توان نمونه جدید ایجاد کرد یا از ServiceLocator استفاده نمود.
        _telemetryClient = new TelemetryClient();
    }

    public void OnException(ExceptionContext context)
    {
        if (context != null && !context.ExceptionHandled)
        {
            try
            {
                if (context.Exception != null)
                {
                    // در ASP.NET Core بررسی وضعیت CustomErrors معمولاً از طریق تنظیمات محیطی یا IWebHostEnvironment انجام می‌شود
                    // یا مستقیماً خطا را ثبت می‌کنیم:
                    _telemetryClient.TrackException(context.Exception);
                }
            }
            catch (Exception exception)
            {
                context.Exception = new AggregateException("Failed to send exception telemetry.", new[] { exception, context.Exception });
            }
        }
    }
}
