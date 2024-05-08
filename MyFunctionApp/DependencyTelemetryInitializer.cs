using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;

public class DependencyTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is DependencyTelemetry dependency)
        {
            if (dependency.Duration.TotalMilliseconds > 10)
            {
                dependency.Success = false;
            }
            else
            {
                dependency.Success = true;
            }
        }
    }
}