#!/bin/bash

# Source configuration variables
source ./0-config.sh
echo "Deploying application in resource group: $RESOURCE_GROUP"

# Create the resource group quietly
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$REGIONS" \
  $TAGS \
  --output none
echo "Resource group created successfully."

# Create the service plan quietly
az appservice plan create \
  --name "$APP_SERVICE_PLAN" \
  --resource-group "$RESOURCE_GROUP" \
  --sku B1 \
  --is-linux \
  --output none
echo "App Service Plan created successfully."

# Create the web app quietly
az webapp create \
  --name "$APP_NAME" \
  --plan "$APP_SERVICE_PLAN" \
  --resource-group "$RESOURCE_GROUP" \
  --runtime "DOTNETCORE|8.0" \
  --deployment-local-git \
  --output none
echo "Web App created successfully."
