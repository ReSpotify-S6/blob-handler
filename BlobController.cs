using Azure;
using BlobHandler.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BlobHandler;

[ApiController]
[Route("")]
[Allow("administrator")]
public class BlobController(IAzureBlobService blobService, ILogger<BlobController> logger, EnvironmentVariableManager envManager) : ControllerBase
{
    [HttpGet]
    public IActionResult FetchBlobs()
    {
        var redirectUri = envManager["REDIRECT_URI"];

        var names = blobService.FetchBlobNames();

        return Ok(names.Select(name => redirectUri + '/' + Uri.EscapeDataString(name)));
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> Download(string name)
    {
        var stream = await blobService.DownloadAsync(name);

        logger.LogInformation("A resource was requested with name '{}'", name);

        if (stream == null)
        {
            logger.LogInformation("A resource with name '{}' was requested, but was not found.", name);
            return NotFound();
        }

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

        string redirectUri = envManager["REDIRECT_URI"];

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