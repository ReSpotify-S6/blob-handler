using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlobHandler.Messaging;
using System.Net;

namespace BlobHandler;

public class AzureBlobService : IAzureBlobService
{
    private readonly BlobContainerClient _containerClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly EnvironmentVariableManager _envManager;

    public AzureBlobService(IEventPublisher publisher, EnvironmentVariableManager envManager)
    {
        var accountName = envManager["AZURE_STORAGE_ACCOUNT_NAME"];
        var accountKey = envManager["AZURE_STORAGE_ACCOUNT_KEY"];
        var containerName = envManager["AZURE_STORAGE_CONTAINER_NAME"];

        var connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

        _containerClient = new BlobContainerClient(connectionString, containerName);
        _eventPublisher = publisher;
        _envManager = envManager;
    }

    public IEnumerable<string> FetchBlobNames()
    {
        _eventPublisher.Publish("test", "Fetching blob names");
        return _containerClient.GetBlobs().Select(item => item.Name);
    }

    public async Task<Stream?> DownloadAsync(string name)
    {
        var blobClient = _containerClient.GetBlobClient(name);
        try
        {
            var result = await blobClient.DownloadStreamingAsync();
            return result.Value.Content;
        }
        catch (RequestFailedException e)
        {
            if (e.Status == (int)HttpStatusCode.NotFound)
            {
                return null;
            }
            throw;
        }
    }

    public async Task<Response<BlobContentInfo>> Upload(string name, Stream data)
    {
        return await _containerClient.UploadBlobAsync(name, data);
    }

    public async void Delete(string name)
    {
        var result = await _containerClient.DeleteBlobIfExistsAsync(name);
        if (result.Value) 
        {
            var uri = new UriBuilder(_envManager["REDIRECT_URI"])
            {
                Path = name
            };
            _eventPublisher.Publish("deleted-blobs", uri);
        }
    }
}
