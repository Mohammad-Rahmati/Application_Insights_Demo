#!/bin/bash

# Source configuration variables
source ./0-config.sh

# Create a storage account for the function app
az storage account create \
    --name $STORAGE_ACCOUNT_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $REGIONS \
    --sku Standard_LRS \
    --output none

# Create a function app for the web app as backend api service
az functionapp create \
    --name $FUNCTION_APP_NAME \
    --storage-account $STORAGE_ACCOUNT_NAME \
    --consumption-plan-location eastus \
    --resource-group $RESOURCE_GROUP \
    --functions-version 4 \
    --runtime python \
    --runtime-version 3.9 \
    --app-insights $APP_INSIGHTS_NAME \
    --os-type Linux \
    --output none


echo "Function app created successfully"