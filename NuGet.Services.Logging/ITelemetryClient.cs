namespace Microsoft.ApplicationInsights;

public interface ITelemetryClient
{
    void TrackMetric(string name, double value, IDictionary<string, string>? properties = null);
}
