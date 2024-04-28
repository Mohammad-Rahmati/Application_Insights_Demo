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
dotnet new webapp -n simple_webapp
```

This command creates a new folder named simple_webapp containing a simple web application.

## Step 1: Run the Setup Script

check the setup variables in the ```0-config.sh``` script and ```chmod +x 0-config.sh```.
```bash
# Set the necessary variables
RESOURCE_GROUP="application-insights-demo-sis"
TAGS='--tags autokillDays=7 reason=testing'
REGIONS="westeurope"
APP_NAME="app-insights-demo-sis"
APP_SERVICE_PLAN="app-insights-demo-sis-plan"
APP_INSIGHTS_NAME="app-insights-demo-sis-insights"
LOCAL_REPO_PATH="simple_webapp/"
```
This script automates the creation of Azure resources necessary for your application, including the deployment of simple_webapp web application to Azure.

```bash
chmod +x 1-InitialSetup.sh
./1-InitialSetup.sh
```

Important:
During the execution of the script, you will be prompted to input a username and password. This is necessary for pushing your application to Azure. You will be provided with the username and password in the terminal. Copy and paste the credentials to push the code and deploy it on Azure web app service.

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

   Navigate to your simple_webapp directory. Run the following command to add the Microsoft Application Insights package to your project:

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
## Step 4: Web App Update to add more functionality for monitoring
 
### Files Modified
- `Index.cshtml`: Razor view file for the home page.
- `Index.cshtml.cs`: C# code-behind file for the home page.

    ### 1. Update `Index.cshtml`
    Replace the existing code in `Index.cshtml` with the following to add a new form and display API responses:

    ```cshtml
    @page
    @model simple_webapp.Pages.IndexModel
    @{
        ViewData["Title"] = "Home page";
    }

    <div class="text-center">
        <h1 class="display-4">Welcome</h1>
        <p>Learn about <a href="https://learn.microsoft.com/aspnet/core">building Web apps with ASP.NET Core</a>.</p>

        <!-- Updated form to include correct handler -->
        <form method="post" asp-page-handler="FetchData">
            <button type="submit" class="btn btn-primary">Fetch Data</button>
        </form>
        
        <!-- Display API response if available -->
        @if (Model.ApiResponse != null)
        {
            <div class="mt-3 alert alert-info">
                API Response: @Model.ApiResponse
            </div>
        }
    </div>
    ```

    ### 2. Update `Index.cshtml.cs`
    ```cs
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    namespace simple_webapp.Pages;

    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
        public string ApiResponse { get; set; }

        public async Task<IActionResult> OnPostFetchDataAsync()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // Generate a random number between 1 and 200
                    Random random = new Random();
                    int randomId = random.Next(1, 201);

                    string apiUrl = $"https://jsonplaceholder.typicode.com/todos/{randomId}";
                    ApiResponse = await client.GetStringAsync(apiUrl);
                }
                catch (System.Exception)
                {
                    ApiResponse = "API call failed.";
                }
            }
            return Page();
        }
    }
    ```