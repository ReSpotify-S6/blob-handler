using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Net;

namespace BlobHandler;

public class AzureBlobService(string connectionString, string containerName) : IAzureBlobService
{
    private readonly BlobContainerClient _containerClient = new(connectionString, containerName);

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

    public void Delete(string name)
    {
        _containerClient.DeleteBlobIfExistsAsync(name);
    }
}
