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
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(IAmazonS3 s3, IConfiguration cfg, ILogger<FileUploadController> logger)
    {
        _s3 = s3;
        _logger = logger;
        _bucket = cfg["S3:BucketName"] ?? throw new InvalidOperationException("S3:BucketName missing");

        _logger.LogInformation("FileUploadController initialized with bucket: {BucketName}", _bucket);
    }

    // POST api/fileupload/upload
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadRequest req)
    {
        _logger.LogInformation("Upload request received");

        try
        {
            if (req.File == null || req.File.Length == 0)
            {
                _logger.LogWarning("Upload request rejected: No file provided");
                return BadRequest("No file provided.");
            }

            var key = string.IsNullOrWhiteSpace(req.KeyPrefix)
                ? req.File.FileName
                : $"{req.KeyPrefix.TrimEnd('/')}/{req.File.FileName}";

            _logger.LogInformation("Starting upload - FileName: {FileName}, Key: {Key}, Size: {FileSize} bytes, ContentType: {ContentType}",
                req.File.FileName, key, req.File.Length, req.File.ContentType);

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

            var response = await _s3.PutObjectAsync(put);

            _logger.LogInformation("File uploaded successfully - Key: {Key}, ETag: {ETag}, Bucket: {Bucket}",
                key, response.ETag, _bucket);

            return Ok(new { bucket = _bucket, key });
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error during file upload - ErrorCode: {ErrorCode}, StatusCode: {StatusCode}",
                ex.ErrorCode, ex.StatusCode);
            return StatusCode(500, $"S3 upload failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file upload");
            return StatusCode(500, "An unexpected error occurred during upload.");
        }
    }

    // GET api/fileupload/download-url/{key}
    [HttpGet("download-url/{key}")]
    public IActionResult GetDownloadUrl([FromRoute] string key, [FromQuery] int minutes = 15)
    {
        _logger.LogInformation("Download URL request received for key: {Key}, expiration minutes: {Minutes}", key, minutes);

        try
        {
            var clampedMinutes = Math.Clamp(minutes, 1, 60);

            if (clampedMinutes != minutes)
            {
                _logger.LogInformation("Expiration minutes clamped from {RequestedMinutes} to {ClampedMinutes}", minutes, clampedMinutes);
            }

            var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(clampedMinutes),
                Verb = HttpVerb.GET
            });

            _logger.LogInformation("Download URL generated successfully for key: {Key}, expires in {Minutes} minutes", key, clampedMinutes);

            return Ok(new { url, expiresInMinutes = clampedMinutes });
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error generating download URL for key: {Key} - ErrorCode: {ErrorCode}, StatusCode: {StatusCode}",
                key, ex.ErrorCode, ex.StatusCode);
            return StatusCode(500, $"Failed to generate download URL: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating download URL for key: {Key}", key);
            return StatusCode(500, "An unexpected error occurred while generating download URL.");
        }
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
