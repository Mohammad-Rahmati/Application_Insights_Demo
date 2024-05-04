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

        // Initialize the telemetry client with the configured settings
        telemetryClient = new TelemetryClient(configuration);
    }

    [FunctionName("HttpTrigger1")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

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
        
        return new OkObjectResult(responseMessage);
    }
}
