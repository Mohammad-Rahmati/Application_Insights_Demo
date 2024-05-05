using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;

public class CustomTelemetryProcessor : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }

    // Constructor to initialize the next processor in the chain
    public CustomTelemetryProcessor(ITelemetryProcessor next)
    {
        this.Next = next;
    }

    public void Process(ITelemetry item)
    {
        // Check if the telemetry item is not an trace telemetry item
        if (!(item is TraceTelemetry))
        {
            // If it is, return early without calling the next processor
            return;
        }
        
        // Otherwise, pass the telemetry item to the next processor in the chain
        Next.Process(item);
    }
}

