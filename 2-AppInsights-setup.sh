#!/bin/bash

# Source configuration variables
source ./0-config.sh

# Create an App Insights resource
az monitor app-insights component create \
    --app $APP_INSIGHTS_NAME \
    --location $REGIONS \
    --resource-group $RESOURCE_GROUP \
    --application-type web \
    --kind web \
    --output none
echo "App Insights resource created"