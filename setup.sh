#! /usr/bin/sh

# Set the necessary variables
RESOURCE_GROUP="application-insights-demo-sis"
TAGS='--tags autokillDays=7 reason=testing'
REGIONS="westeurope"
APP_NAME="app-insights-demo-sis"
APP_SERVICE_PLAN="app-insights-demo-sis-plan"
APP_INSIGHTS_NAME="app-insights-demo-sis-insights"
LOCAL_REPO_PATH="WebAppWithoutAppInsights/"  # Modify this path to your local repo

# Create the resource group quietly
az group create --name "$RESOURCE_GROUP" --location "$REGIONS" $TAGS --output none
echo "Resource group created successfully."
# Create the resources quietly
az appservice plan create --name "$APP_SERVICE_PLAN" --resource-group "$RESOURCE_GROUP" --sku B1 --is-linux --output none
echo "App Service Plan created successfully."

az monitor app-insights component create --app "$APP_INSIGHTS_NAME" --resource-group "$RESOURCE_GROUP" --location "$REGIONS" --kind web --output none
echo "Application Insights created successfully."

# Retrieve the Application Insights connection string quietly
INSTRUMENTATION_KEY=$(az monitor app-insights component show --app "$APP_INSIGHTS_NAME" --resource-group "$RESOURCE_GROUP" --query connectionString --output tsv) > /dev/null 2>&1

# Create the web app quietly
az webapp create --name "$APP_NAME" --plan "$APP_SERVICE_PLAN" --resource-group "$RESOURCE_GROUP" --runtime "DOTNETCORE|8.0" --deployment-local-git --output none
echo "Web App created successfully."

# Set Application Insights connection string in the web app's application settings quietly
az webapp config appsettings set --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --settings APPINSIGHTS_INSTRUMENTATIONKEY="$INSTRUMENTATION_KEY" --output none
echo "Application Insights connection string set successfully."

# Enable local Git deployment for the web app
DEPLOYMENT_URL=$(az webapp deployment source config-local-git --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --query url --output tsv) || exit 1
DEPLOYMENT_URL=$(echo $DEPLOYMENT_URL | sed 's/None@//g')
GIT_USERNAME="$""${APP_NAME}"
GIT_PASSWORD=$(az webapp deployment list-publishing-credentials --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --output json | grep "publishingPassword" | sed -n 's/.*"publishingPassword": "\([^"]*\)".*/\1/p') || exit 1


# Navigate to local repository and configure Git deployment
cd "$LOCAL_REPO_PATH" || exit 1
git init
git add .
git commit -m "Initial commit"
git remote remove azure 2> /dev/null  # Remove if exists to prevent errors

echo "git url: $DEPLOYMENT_URL"
echo "git username: $GIT_USERNAME"
echo "git password: $GIT_PASSWORD"

# Git operations
git remote add azure $DEPLOYMENT_URL
git push azure master || { echo 'Failed to push to Azure'; exit 1; }