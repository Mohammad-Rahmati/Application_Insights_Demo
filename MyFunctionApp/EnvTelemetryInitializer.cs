using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

public class EnvironmentTelemetryInitializer : ITelemetryInitializer
{
    private string _environment;

    public EnvironmentTelemetryInitializer(string environment)
    {
        _environment = environment;
    }

    public void Initialize(ITelemetry telemetry)
    {
        if (_environment == "dev")
        {
            telemetry.Context.Properties["Environment"] = "Development";
        }
        else if (_environment == "test")
        {
            telemetry.Context.Properties["Environment"] = "Test";
        }
    }
}