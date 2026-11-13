// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.ApplicationInsights.Extensibility;

namespace NuGet.Services.Logging;

public static class TelemetryConfigurationExtensions
{
    /// <summary>
    /// Placeholder for configuration extensions in .NET Core.
    /// </summary>
    public static bool AddOrSetHeartbeatProperty(
        this TelemetryConfiguration configuration,
        string propertyName,
        string propertyValue,
        bool isHealthy)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        // در .NET Core تنظیمات تله‌متری به صورت خودکار مدیریت می‌شوند
        return true;
    }
}
