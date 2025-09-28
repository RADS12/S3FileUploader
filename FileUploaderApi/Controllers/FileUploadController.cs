using Amazon.S3;
using Amazon.S3.Model;
using FileUploaderApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace FileUploaderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly ILogger<FileUploadController> _logger;
    private readonly IConfiguration _configuration;

    // Security constants
    private const long MaxFileSize = 500_000_000; // 500MB
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp",
        "application/pdf", "text/plain", "application/json",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/zip", "application/x-zip-compressed"
    };

    public FileUploadController(IAmazonS3 s3, IConfiguration cfg, ILogger<FileUploadController> logger)
    {
        _s3 = s3;
        _logger = logger;
        _configuration = cfg;
        _bucket = cfg["S3:BucketName"] ?? throw new InvalidOperationException("S3:BucketName missing");

        _logger.LogInformation("FileUploadController initialized with bucket: {BucketName}", _bucket);
    }

    /// <summary>
    /// Sanitizes file names to prevent path traversal and injection attacks
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return $"file_{Guid.NewGuid():N}";

        // Remove path characters and dangerous characters
        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat(new[] { '/', '\\' })
            .Concat(new[] { '<', '>', '|', ':', '*', '?' })
            .ToArray();

        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Ensure we have a valid filename
        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Length > 255)
        {
            var extension = Path.GetExtension(fileName);
            sanitized = $"file_{Guid.NewGuid():N}{extension}";
        }

        return sanitized;
    }

    /// <summary>
    /// Validates file upload request
    /// </summary>
    private IActionResult? ValidateUploadRequest(UploadRequest req)
    {
        // Check file size
        if (req.File.Length > MaxFileSize)
        {
            _logger.LogWarning("Upload rejected: File too large - {FileSize} bytes (max: {MaxSize})",
                req.File.Length, MaxFileSize);
            return BadRequest($"File size exceeds {MaxFileSize / 1_000_000}MB limit.");
        }

        // Check content type
        if (!string.IsNullOrEmpty(req.File.ContentType) &&
            !AllowedContentTypes.Contains(req.File.ContentType))
        {
            _logger.LogWarning("Upload rejected: Invalid content type - {ContentType}", req.File.ContentType);
            return BadRequest($"Content type '{req.File.ContentType}' is not allowed.");
        }

        return null; // Valid request
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        try
        {
            // Simple health check - could be expanded to check S3 connectivity
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "FileUploadController",
                bucket = _bucket
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { status = "unhealthy", timestamp = DateTime.UtcNow });
        }
    }
    // POST api/fileupload/upload
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<IActionResult> Upload([FromForm] UploadRequest req, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Upload request received");

        try
        {
            if (req.File == null || req.File.Length == 0)
            {
                _logger.LogWarning("Upload request rejected: No file provided");
                return BadRequest("No file provided.");
            }

            // Validate the upload request
            var validationResult = ValidateUploadRequest(req);
            if (validationResult != null)
                return validationResult;

            // Sanitize file name to prevent security issues
            var sanitizedFileName = SanitizeFileName(req.File.FileName);
            var key = string.IsNullOrWhiteSpace(req.KeyPrefix)
                ? sanitizedFileName
                : $"{req.KeyPrefix.TrimEnd('/')}/{sanitizedFileName}";

            _logger.LogInformation("Starting upload - OriginalFileName: {OriginalFileName}, SanitizedFileName: {SanitizedFileName}, Key: {Key}, Size: {FileSize} bytes, ContentType: {ContentType}",
                req.File.FileName, sanitizedFileName, key, req.File.Length, req.File.ContentType); await using var stream = req.File.OpenReadStream();
            var put = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                AutoCloseStream = true,
                ContentType = req.File.ContentType ?? "application/octet-stream",
                Metadata = {
                    ["uploaded-by"] = _configuration["Upload:DefaultUploader"] ?? "System",
                    ["uploaded-at"] = DateTime.UtcNow.ToString("O"),
                    ["original-filename"] = req.File.FileName ?? "unknown",
                    ["file-size"] = req.File.Length.ToString()
                }
            };

            var response = await _s3.PutObjectAsync(put, cancellationToken);

            _logger.LogInformation("File uploaded successfully - Key: {Key}, ETag: {ETag}, Bucket: {Bucket}",
                key, response.ETag, _bucket);

            return Ok(new UploadResponse(
                _bucket,
                key,
                req.File.Length,
                req.File.ContentType ?? "application/octet-stream",
                DateTime.UtcNow
            ));
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error during file upload - ErrorCode: {ErrorCode}, StatusCode: {StatusCode}",
                ex.ErrorCode, ex.StatusCode);
            return StatusCode(500, "Upload failed. Please try again later.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Upload operation was cancelled");
            return StatusCode(499, "Upload was cancelled.");
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
        // URL decode the key to handle special characters
        var decodedKey = Uri.UnescapeDataString(key);
        _logger.LogInformation("Download URL request received for key: {Key}, expiration minutes: {Minutes}", decodedKey, minutes);

        try
        {
            if (string.IsNullOrWhiteSpace(decodedKey))
            {
                _logger.LogWarning("Download URL request rejected: Empty key provided");
                return BadRequest("File key is required.");
            }

            var clampedMinutes = Math.Clamp(minutes, 1, 60);

            if (clampedMinutes != minutes)
            {
                _logger.LogInformation("Expiration minutes clamped from {RequestedMinutes} to {ClampedMinutes}", minutes, clampedMinutes);
            }

            var expiresAt = DateTime.UtcNow.AddMinutes(clampedMinutes);
            var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = decodedKey,
                Expires = expiresAt,
                Verb = HttpVerb.GET
            });

            _logger.LogInformation("Download URL generated successfully for key: {Key}, expires in {Minutes} minutes", decodedKey, clampedMinutes);

            return Ok(new DownloadUrlResponse(url, clampedMinutes, expiresAt));
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error generating download URL for key: {Key} - ErrorCode: {ErrorCode}, StatusCode: {StatusCode}",
                decodedKey, ex.ErrorCode, ex.StatusCode);
            return StatusCode(500, "Failed to generate download URL. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating download URL for key: {Key}", decodedKey);
            return StatusCode(500, "An unexpected error occurred while generating download URL.");
        }
    }

    // POST api/fileupload/upload-url (client can PUT directly to S3)
    [HttpPost("upload-url")]
    public IActionResult GetUploadUrl([FromBody] PresignUploadRequest body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Upload URL request received for key: {DesiredKey}, expiration minutes: {Minutes}",
            body.DesiredKey, body.Minutes);

        try
        {
            // Validate the request
            if (body.Minutes < 1 || body.Minutes > 60)
            {
                _logger.LogWarning("Upload URL request rejected: Invalid expiration minutes - {Minutes}", body.Minutes);
                return BadRequest("Expiration minutes must be between 1 and 60.");
            }

            // Generate a secure key
            var key = string.IsNullOrWhiteSpace(body.DesiredKey)
                ? $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}"
                : SanitizeFileName(body.DesiredKey);

            var clampedMinutes = Math.Clamp(body.Minutes, 1, 60);
            var expiresAt = DateTime.UtcNow.AddMinutes(clampedMinutes);

            var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = key,
                Expires = expiresAt,
                Verb = HttpVerb.PUT,
                ContentType = body.ContentType ?? "application/octet-stream"
            });

            _logger.LogInformation("Upload URL generated successfully for key: {Key}, expires in {Minutes} minutes",
                key, clampedMinutes);

            return Ok(new UploadUrlResponse(key, url, clampedMinutes, expiresAt));
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error generating upload URL - ErrorCode: {ErrorCode}, StatusCode: {StatusCode}",
                ex.ErrorCode, ex.StatusCode);
            return StatusCode(500, "Failed to generate upload URL. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating upload URL");
            return StatusCode(500, "An unexpected error occurred while generating upload URL.");
        }
    }

    /// <summary>
    /// Request model for generating presigned upload URLs
    /// </summary>
    /// <param name="DesiredKey">Optional desired S3 key. If not provided, a unique key will be generated.</param>
    /// <param name="Minutes">URL expiration time in minutes (1-60). Default is 15.</param>
    /// <param name="ContentType">Optional content type for the upload. Default is application/octet-stream.</param>
    public record PresignUploadRequest(
        string? DesiredKey = null,
        int Minutes = 15,
        string? ContentType = null
    );
}

// Response DTOs
public record UploadResponse(string Bucket, string Key, long FileSize, string ContentType, DateTime UploadedAt);
public record DownloadUrlResponse(string Url, int ExpiresInMinutes, DateTime ExpiresAt);
public record UploadUrlResponse(string Key, string Url, int ExpiresInMinutes, DateTime ExpiresAt);
