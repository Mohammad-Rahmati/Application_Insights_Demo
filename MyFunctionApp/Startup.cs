using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;

[assembly: FunctionsStartup(typeof(MyFunctionApp.Startup))]

namespace MyFunctionApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Registers the custom telemetry processor with the dependency injection container
            builder.Services.AddApplicationInsightsTelemetryProcessor<CustomTelemetryProcessor>();
            // builder.Services.AddSingleton<ITelemetryInitializer, DependencyTelemetryInitializer>();
        }
    }
}