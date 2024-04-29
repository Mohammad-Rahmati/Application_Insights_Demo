# Installation of Application Insights on Azure for .NET Core Web Application

This repository contains the necessary tools and scripts to deploy a .NET Core web application integrated with Application Insights on Azure. This guide will walk you through creating a new .NET Core web application, setting up Azure resources via a script, and deploying the application with monitoring enabled.

## Prerequisites

Before you begin, ensure you have the following installed:
- [.NET Core SDK](https://dotnet.microsoft.com/download)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- An active Azure subscription.

## Step 0: Create a .NET Core Web Application

Start by creating a new .NET Core web application on your local machine. Open your terminal and run the following command:

```bash
dotnet new webapp -n SimpleWebApp
```

This command creates a new folder named SimpleWebApp containing a simple web application.

## Step 1: Run the Setup Script

check the setup variables in the ```0-config.sh``` script and ```chmod +x 0-config.sh```.
```bash
# Configuration variables for the Azure setup
export RESOURCE_GROUP="application-insights-demo-sis"
export TAGS='--tags autokillDays=7 reason=testing'
export REGIONS="westeurope"
export APP_NAME="app-insights-demo-sis"
export APP_SERVICE_PLAN="app-insights-demo-sis-plan"
export APP_INSIGHTS_NAME="app-insights-demo-sis-insights"
export FUNCTION_APP_NAME="app-insights-demo-sis-func"
export STORAGE_ACCOUNT_NAME="appinsightstoragesis"
export LOCAL_REPO_PATH="SimpleWebApp/"
```
This script automates the creation of Azure resources necessary for your application, including the deployment of SimpleWebApp web application to Azure.

```bash
chmod +x 1-InitialSetup.sh
./1-InitialSetup.sh
```

Important:
During the execution of the script, you will be prompted to input a username and password. This is necessary for pushing your application to Azure. 
Find the Git Clone Uri, local username, and password from Deployment Center on Web App reouce on Azure. Navigate to local SimpleWebApp repository and configure Git deployment
```bash
git init
git add .
git commit -m "Initial commit"
git remote remove azure 2> /dev/null  # Remove if exists to prevent errors
git config --global --unset credential.helper 2> /dev/null  # Unset to prevent errors
git config --global credential.helper 'cache --timeout=36000'

git remote add azure <DEPLOYMENT_URL>
git push azure master
```
## Step 2: Setup Application Insights
```bash
chmod +x 2-AppInsights-setup.sh
./2-AppInsights-setup.sh
```
This command creates an Application Insights resource with the type and kind set to 'web', which is suitable for monitoring web applications.

## Step 3: Setting Up Application Insights

To monitor and track the performance of your application, we will integrate Microsoft Application Insights. Follow these steps to configure Application Insights:

Fetch the connection string from Azure:

```
source ./0-config.sh
az monitor app-insights component show --app $APP_INSIGHTS_NAME --resource-group $RESOURCE_GROUP --query connectionString -o tsv
```

1. **Add the Application Insights Package:**

   Navigate to your SimpleWebApp directory. Run the following command to add the Microsoft Application Insights package to your project:

   ```bash
   dotnet add package Microsoft.ApplicationInsights.AspNetCore
   ```
2. **Configure Application Insights in appsettings.json:**
    Add the following JSON configuration to your ```appsettings.json``` file. This configuration includes the connection string with your unique InstrumentationKey, IngestionEndpoint, LiveEndpoint, and ApplicationId. Make sure to replace the placeholders with your actual Application Insights values:
    ```json
    {
    "Logging": {
        "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
        }
    },
    "ApplicationInsights": {
        "ConnectionString": "INSERT_CONNECTION_STRING_HERE"
    },
    "AllowedHosts": "*"
    }

    ```
    Add the Application Insight service to the ```Program.cs```:

    ```
    builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["ApplicationInsights:ConnectionString"]);
    ```

## Step 4: Create a Function App as Backend service and internal API for the background jobs

This script automates the creation of Azure resources necessary for your Function App, including the deployment of Storage Account and Function App to Azure.

```bash
chmod +x 4-FunctionApp.sh
./4-FunctionApp.sh
```

To create a Function App for handling background jobs and internal API tasks, ensure you have the Azure Functions Core Tools installed. Follow these steps to set up and deploy your Function App:

### Install Azure Functions Core Tools:
Here's the modified sentence with a Markdown link:

If you haven't already installed Azure Functions Core Tools, you can do so by following the installation guide [here](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=linux%2Cisolated-process%2Cnode-v4%2Cpython-v2%2Chttp-trigger%2Ccontainer-apps&pivots=programming-language-csharp).
### Create and Configure the Function App:
Use the Azure CLI to create a new Function App and its related resources. Navigate to your working directory and execute the following commands:

```bash
func init MyFunctionApp --worker-runtime dotnet
cd MyFunctionApp
func new --name MyHttpFunction --template "HTTPTrigger"
dotnet add package Newtonsoft.Json
```

This sets up a new Azure Function App with a single HTTP-triggered function. The --authlevel "anonymous" parameter allows the function to be triggered without authentication, which is suitable for internal use.

### Files to Modify
- `MyHttpFunction.cs`: This file contains the main Azure Function code that processes HTTP requests and interacts with Azure Blob Storage.
- `local.settings.json`: This file holds all your local configuration settings, including connection strings and application settings.

    ### 1. Update `MyHttpFunction.cs`
    Replace the existing code in `MyHttpFunction.cs` with the new code below that defines an HTTP-triggered function capable of receiving requests, processing names, and storing messages in Azure Blob Storage:

    ```cs
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

    public static class HttpTrigger1
    {
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

            return new OkObjectResult(responseMessage);
        }
    }

    ```

    ### 2. Update `local.settings.json`
    Enter the Azure Storage account connection string in local.settings.json, which you can find in your Azure Storage account keys:

    ```json
    {
        "IsEncrypted": false,
        "Values": {
            "AzureWebJobsStorage": "INSERT_CONNECTION_STRING_HERE",
            "FUNCTIONS_WORKER_RUNTIME": "dotnet"
        }
    }
    ```

### Deploy the Function App:
```bash
source ./0-config.sh
cd $FUNCTION_APP_PATH
func azure functionapp publish $FUNCTION_APP_NAME
```

## Step 5: Add more functionality to the webapp
 
### Files to Modify
- `Index.cshtml`: Razor view file for the home page.
- `Index.cshtml.cs`: C# code-behind file for the home page.

    ### 1. Update `Index.cshtml`
    Replace the existing code in `Index.cshtml` with the following to add a new form and display API responses:

    ```html
    @page
    @model SimpleWebApp.Pages.IndexModel
    @{
        ViewData["Title"] = "Home page";
    }

    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>@ViewData["Title"]</title>
        <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css">
        <style>
            body { padding: 20px; }
            .header { margin-bottom: 40px; }
            .alert { margin-top: 20px; }
        </style>
    </head>
    <body>
        <div class="container">
            <div class="text-center header">
                <h1 class="display-4">Welcome</h1>
                <p>Learn about <a href="https://learn.microsoft.com/aspnet/core">building Web apps with ASP.NET Core</a>.</p>
            </div>

            <form method="post" class="form-inline justify-content-center">
                <button type="submit" asp-page-handler="FetchData" class="btn btn-primary mr-2">Fetch Data</button>
                
                <div class="form-group">
                    <label for="name" class="mr-2">Name:</label>
                    <input type="text" id="name" name="name" class="form-control mr-2" placeholder="Enter your name">
                    <button type="submit" asp-page-handler="FetchDataWithName" class="btn btn-success">Fetch Data with Name</button>
                </div>
            </form>
            
            @if (Model.ApiResponse != null)
            {
                <div class="alert alert-info">
                    API Response: @Model.ApiResponse
                </div>
            }

            @if (Model.CustomApiResponse != null)
            {
                <div class="alert alert-success">
                    Custom API Response: @Model.CustomApiResponse
                </div>
            }
        </div>
    </body>
    </html>
    ```

    ### 2. Update `Index.cshtml.cs`
    make sure to provide the invokation API for the customApiUrl.
    ```cs
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web;

    namespace SimpleWebApp.Pages;

    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public string Name { get; set; }

        public string ApiResponse { get; set; }
        public string CustomApiResponse { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostFetchDataAsync()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    Random random = new Random();
                    int randomId = random.Next(1, 201);
                    string apiUrl = $"https://jsonplaceholder.typicode.com/todos/{randomId}";
                    ApiResponse = await client.GetStringAsync(apiUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API call failed.");
                    ApiResponse = "API call failed.";
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostFetchDataWithNameAsync()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var encodedName = HttpUtility.UrlEncode(Name);
                    string customApiUrl = $"INVOKATION_API&name={encodedName}";
                    CustomApiResponse = await client.GetStringAsync(customApiUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Custom API call failed.");
                    CustomApiResponse = "Custom API call failed.";
                }
            }
            return Page();
        }
    }

    ```