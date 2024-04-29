import azure.functions as func
import logging
import uuid
import os
from azure.storage.blob import BlobServiceClient, BlobClient, ContainerClient

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)

@app.route(route="http_trigger1")
def http_trigger1(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    # Fetch the connection string from environment variables
    connect_str =  os.getenv('AzureWebJobsStorage')
    blob_service_client = BlobServiceClient.from_connection_string(connect_str)

    try:
        container_client = blob_service_client.get_container_client('azure-webjobs-container')
    except:
        container_client = blob_service_client.create_container('azure-webjobs-container')

    blob_client = blob_service_client.get_blob_client(container='azure-webjobs-container', blob=f'{str(uuid.uuid4())}.txt')

    name = req.params.get('name')
    if not name:
        try:
            req_body = req.get_json()
        except ValueError:
            pass
        else:
            name = req_body.get('name')
    response_message = ""
    if name:
        response_message = f"Hello, {name}. This HTTP triggered function executed successfully."
        # Writing the name to the blob, consider using append blob for continuous data addition
        blob_client.upload_blob(f"Hello, {name}", overwrite=True)
    else:
        response_message = "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
        # Write a default message if no name is provided
        blob_client.upload_blob("Request received but no name provided", overwrite=True)

    return func.HttpResponse(response_message, status_code=200)