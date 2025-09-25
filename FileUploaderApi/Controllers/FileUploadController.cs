using Amazon.S3;
using Amazon.S3.Model;
using FileUploaderApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FileUploaderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public FileUploadController(IAmazonS3 s3, IConfiguration cfg)
    {
        _s3 = s3;
        _bucket = cfg["S3:BucketName"] ?? throw new InvalidOperationException("S3:BucketName missing");
    }

    // POST api/fileupload/upload
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadRequest req)
    {
        if (req.File == null || req.File.Length == 0)
            return BadRequest("No file provided.");

        var key = string.IsNullOrWhiteSpace(req.KeyPrefix)
            ? req.File.FileName
            : $"{req.KeyPrefix.TrimEnd('/')}/{req.File.FileName}";

        using var stream = req.File.OpenReadStream();
        var put = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            AutoCloseStream = true,
            ContentType = req.File.ContentType,
            Metadata = { ["uploaded-by"] = "Rad" }
        };

        await _s3.PutObjectAsync(put);

        return Ok(new { bucket = _bucket, key });
    }
    
    // GET api/fileupload/download-url/{key}
    [HttpGet("download-url/{key}")]
    public IActionResult GetDownloadUrl([FromRoute] string key, [FromQuery] int minutes = 15)
    {
        var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(Math.Clamp(minutes, 1, 60)),
            Verb = HttpVerb.GET
        });
        return Ok(new { url, expiresInMinutes = Math.Clamp(minutes, 1, 60) });
    }

    // POST api/fileupload/upload-url  (client can PUT directly to S3)
    //[HttpPost("upload-url")]
    // public IActionResult GetUploadUrl([FromBody] PresignUploadRequest body)
    // {
    //     var key = string.IsNullOrWhiteSpace(body.DesiredKey)
    //         ? $"{Guid.NewGuid():N}"
    //         : body.DesiredKey;

    //     var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
    //     {
    //         BucketName = _bucket,
    //         Key = key,
    //         Expires = DateTime.UtcNow.AddMinutes(Math.Clamp(body.Minutes, 1, 60)),
    //         Verb = HttpVerb.PUT
    //     });

    //     return Ok(new { key, url, expiresInMinutes = Math.Clamp(body.Minutes, 1, 60) });
    // }

    //public record PresignUploadRequest(string? DesiredKey, int Minutes = 15);
}
