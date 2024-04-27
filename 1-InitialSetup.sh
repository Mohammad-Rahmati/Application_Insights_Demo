#!/bin/bash

# Source configuration variables
source ./0-config.sh
echo "Deploying application in resource group: $RESOURCE_GROUP"

# Create the resource group quietly
az group create --name "$RESOURCE_GROUP" --location "$REGIONS" $TAGS --output none
echo "Resource group created successfully."

# Create the service plan quietly
az appservice plan create --name "$APP_SERVICE_PLAN" --resource-group "$RESOURCE_GROUP" --sku B1 --is-linux --output none
echo "App Service Plan created successfully."

# Create the web app quietly
az webapp create --name "$APP_NAME" --plan "$APP_SERVICE_PLAN" --resource-group "$RESOURCE_GROUP" --runtime "DOTNETCORE|8.0" --deployment-local-git --output none
echo "Web App created successfully."

# Enable local Git deployment for the web app quietly
DEPLOYMENT_URL=$(az webapp deployment source config-local-git --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --query url --output tsv) || exit 1
DEPLOYMENT_URL=$(echo $DEPLOYMENT_URL | sed 's/None@//g')
GIT_USERNAME="$""${APP_NAME}"
GIT_PASSWORD=$(az webapp deployment list-publishing-credentials --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --output json \
    | grep "publishingPassword" | sed -n 's/.*"publishingPassword": "\([^"]*\)".*/\1/p') || exit 1

# Navigate to local repository and configure Git deployment
cd "$LOCAL_REPO_PATH" || exit 1
git init
git add .
git commit -m "Initial commit"
git remote remove azure 2> /dev/null  # Remove if exists to prevent errors
git config --global --unset credential.helper 2> /dev/null  # Unset to prevent errors
git config --global credential.helper 'cache --timeout=36000'

echo "git url: $DEPLOYMENT_URL"
echo "git username: $GIT_USERNAME"
echo "git password: $GIT_PASSWORD"

# Git operations
git remote add azure $DEPLOYMENT_URL
git push azure master