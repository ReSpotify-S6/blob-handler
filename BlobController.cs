using Azure;
using BlobHandler.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BlobHandler;

[ApiController]
[Route("/")]
[Allow("administrator")]
public class BlobController(IAzureBlobService blobService) : ControllerBase
{
    [HttpGet]
    public IActionResult FetchBlobs()
    {
        return Ok(blobService.FetchBlobNames());
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> Download(string name)
    {
        var stream = await blobService.DownloadAsync(name);

        if (stream == null)
            return NotFound();

        var contentType = GetContentType(name);

        return File(stream, contentType, name);
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tiff" => "image/tiff",

            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".aac" => "audio/aac",
            ".flac" => "audio/flac",
            _ => "application/octet-stream"
        };
    }

    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] UploadFormat formData)
    {
        var result = blobService.Upload(formData.Name, formData.File.OpenReadStream());

        try
        {
            await result;
        }
        catch(RequestFailedException ex)
        {
            return (HttpStatusCode)ex.Status switch
            {
                HttpStatusCode.Conflict => Conflict(),
                _ => StatusCode(ex.Status)
            };
        }

        string redirectUri = Environment.GetEnvironmentVariable("REDIRECT_URI")!;

        var uriBuilder = new UriBuilder(redirectUri)
        {
            Path = $"/{formData.Name}"
        };

        return Created(uriBuilder.Uri, null);
    }

    [HttpDelete("{name}")]
    public IActionResult Delete(string name)
    {
        blobService.Delete(name);
        return NoContent();
    }
}