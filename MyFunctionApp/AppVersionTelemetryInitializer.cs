using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

public class AppVersionTelemetryInitializer : ITelemetryInitializer
{
    private readonly string _appVersion;

    // Constructor to pass the application version
    public AppVersionTelemetryInitializer(string appVersion)
    {
        _appVersion = appVersion;
    }

    public void Initialize(ITelemetry telemetry)
    {
        // Set the application version in the telemetry context
        if (string.IsNullOrEmpty(telemetry.Context.Component.Version))
        {
            telemetry.Context.Component.Version = _appVersion;
        }
    }
}