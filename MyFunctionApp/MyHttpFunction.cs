using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System.Collections.Generic; // Required for using Dictionary
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

public static class HttpTrigger1
{
    private static TelemetryClient telemetryClient;
    static HttpTrigger1()
    {
        // Retrieve the Application Insights connection string from environment variables
        string connectionString = Environment.GetEnvironmentVariable("APP_INSIGHTS_CONNECTION_STRING");

        // Create a default telemetry configuration
        TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();

        // Assign the retrieved connection string to the configuration
        configuration.ConnectionString = connectionString;
        
        QuickPulseTelemetryProcessor quickPulseProcessor = null;

        // Setup the telemetry processor chain, adding CustomTelemetryProcessor first
        configuration.TelemetryInitializers.Add(new AppVersionTelemetryInitializer("1.0.2"));
        configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder
            .Use((next) => new CustomTelemetryProcessor(next)) // Add the custom processor first to filter out exceptions
            .Use((next) => {
                quickPulseProcessor = new QuickPulseTelemetryProcessor(next); // Setup QuickPulseTelemetryProcessor next
                return quickPulseProcessor;
            })
            .UseSampling(100)
            .Build();

        var quickPulseModule = new QuickPulseTelemetryModule();
        quickPulseModule.Initialize(configuration);
        quickPulseModule.RegisterTelemetryProcessor(quickPulseProcessor); // Register QuickPulseTelemetryProcessor in the QuickPulse module

        // Initialize the telemetry client with the configured settings
        telemetryClient = new TelemetryClient(configuration);
    }

    [FunctionName("HttpTrigger1")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
        CloudBlobContainer container = blobClient.GetContainerReference("azure-webjobs-container");
        await container.CreateIfNotExistsAsync();

        string name = req.Query["name"];
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        name = name ?? data?.name;

        string responseMessage = string.IsNullOrEmpty(name)
            ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            : $"Hello, {name}. This HTTP triggered function executed successfully.";

        CloudBlockBlob blockBlob = container.GetBlockBlobReference($"{Guid.NewGuid()}.txt");
        await blockBlob.UploadTextAsync(responseMessage);
        
        // Create a dictionary to hold custom properties
        Dictionary<string, string> customProperties = new Dictionary<string, string>();
        customProperties.Add("BlobName", blockBlob.Name);
        customProperties.Add("BlobUri", blockBlob.Uri.ToString());

        // Track a custom event with the custom properties
        telemetryClient.TrackEvent("BlobCreated", customProperties);
        timer.Stop();
        var telemetry = new RequestTelemetry
        {
            Name = $"{req.Method} {req.Path}",
            Timestamp = startTime,
            Duration = timer.Elapsed,
            ResponseCode = "200", // Adjust based on actual response
            Success = true,
            Url = new Uri(req.GetDisplayUrl())
        };
        telemetryClient.TrackRequest(telemetry);

        // An example exception to Application Insights
        try
        {
            throw new Exception("Example exception");
        }
        catch (Exception ex)
        {
            telemetryClient.TrackException(ex);
        }

        // Send dependency telemetry to Application Insights
        var dependencyTelemetry = new DependencyTelemetry
        {
            Name = "AzureBlob",
            Target = "AzureBlob",
            Data = "AzureBlob",
            Timestamp = startTime,
            Duration = timer.Elapsed,
            Success = true
        };
        telemetryClient.TrackDependency(dependencyTelemetry);

        // Send a custom metric to Application Insights
        var metric = new MetricTelemetry("CustomMetric", 1);
        metric.Properties.Add("Detail", "Additional Info");
        telemetryClient.TrackMetric(metric);

        // track a custom trace
        // telemetryClient.TrackTrace("This is a custom trace message");
        // a loop of 1000 same trace messages
        for (int i = 0; i < 1; i++)
        {
            telemetryClient.TrackTrace("This is a custom trace message");
        }
        // Flush the telemetry to ensure that it is sent to Application Insights
        telemetryClient.Flush();
        return new OkObjectResult(responseMessage);
    }
}