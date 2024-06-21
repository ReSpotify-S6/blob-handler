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
    private readonly IReadOnlyDictionary<string, string> _envStore;
    private readonly ILogger _logger;

    public AzureBlobService(IEventPublisher publisher, IReadOnlyDictionary<string, string> envStore, ILogger<AzureBlobService> logger)
    {
        var accountName = envStore["AZURE_STORAGE_ACCOUNT_NAME"];
        var accountKey = envStore["AZURE_STORAGE_ACCOUNT_KEY"];
        var containerName = envStore["AZURE_STORAGE_CONTAINER_NAME"];

        var connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

        _containerClient = new BlobContainerClient(connectionString, containerName);
        _eventPublisher = publisher;
        _envStore = envStore;
        _logger = logger;
    }

    public IEnumerable<string> FetchBlobNames()
    {
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
        _logger.LogInformation("Deleting resource with name '{}'...", name);
        var result = await _containerClient.DeleteBlobIfExistsAsync(name);
        if (result.Value) 
        {
            var uri = $"{_envStore["REDIRECT_URI"]}/{Uri.EscapeDataString(name)}";
            _eventPublisher.Publish("deleted-blobs", uri);
            _logger.LogInformation("Published deleted resource uri '{}'", uri);
        }
    }
}
