using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

namespace NuGet.Services.Logging;

public class ApplicationInsightsConfiguration
{
    public TelemetryConfiguration TelemetryConfiguration { get; }
    public DiagnosticsTelemetryModule DiagnosticsTelemetryModule { get; }
    public DependencyTrackingTelemetryModule? DependencyTrackingTelemetryModule { get; }

    public ApplicationInsightsConfiguration(
        TelemetryConfiguration telemetryConfiguration,
        DiagnosticsTelemetryModule diagnosticsTelemetryModule,
        DependencyTrackingTelemetryModule? dependencyTrackingTelemetryModule)
    {
        TelemetryConfiguration = telemetryConfiguration;
        DiagnosticsTelemetryModule = diagnosticsTelemetryModule;
        DependencyTrackingTelemetryModule = dependencyTrackingTelemetryModule;
    }
}
