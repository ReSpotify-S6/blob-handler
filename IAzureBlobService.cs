using Azure;
using Azure.Storage.Blobs.Models;

namespace BlobHandler
{
    public interface IAzureBlobService
    {
        void Delete(string name);
        Task<Stream?> DownloadAsync(string name);
        IEnumerable<string> FetchBlobNames();
        Task<Response<BlobContentInfo>> Upload(string name, Stream data);
    }
}