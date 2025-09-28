using Amazon.DynamoDBv2.Model;
using FileUploaderApi.Models;
using FileUploaderApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileUploaderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DynamoFileController : ControllerBase
{
    private readonly IDynamoFileService _fileService;
    private readonly ILogger<DynamoFileController> _logger;

    // Security constants
    private const long MaxFileSize = 10_000_000; // 10MB (DynamoDB item limit is 400KB, but we'll use 10MB for demo)
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp",
        "application/pdf", "text/plain", "application/json", "text/csv",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/zip", "application/x-zip-compressed"
    };

    public DynamoFileController(IDynamoFileService fileService, ILogger<DynamoFileController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint for DynamoDB file service
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        try
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "DynamoFileController",
                storage = "DynamoDB"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { status = "unhealthy", timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Upload file to DynamoDB
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<IActionResult> UploadFile([FromForm] DynamoUploadRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DynamoDB upload request received");

        try
        {
            if (request.File == null || request.File.Length == 0)
            {
                _logger.LogWarning("Upload request rejected: No file provided");
                return BadRequest("No file provided.");
            }

            // Validate file size (DynamoDB has 400KB item limit, but we'll be more restrictive)
            if (request.File.Length > MaxFileSize)
            {
                _logger.LogWarning("Upload rejected: File too large - {FileSize} bytes (max: {MaxSize})",
                    request.File.Length, MaxFileSize);
                return BadRequest($"File size exceeds {MaxFileSize / 1_000_000}MB limit.");
            }

            // Validate content type
            if (!string.IsNullOrEmpty(request.File.ContentType) &&
                !AllowedContentTypes.Contains(request.File.ContentType))
            {
                _logger.LogWarning("Upload rejected: Invalid content type - {ContentType}", request.File.ContentType);
                return BadRequest($"Content type '{request.File.ContentType}' is not allowed.");
            }

            var fileId = await _fileService.UploadFileAsync(request, cancellationToken);

            _logger.LogInformation("File uploaded successfully to DynamoDB - Id: {FileId}, Size: {FileSize} bytes",
                fileId, request.File.Length);

            return Ok(new DynamoUploadResponse(
                fileId,
                request.File.FileName ?? "unknown",
                SanitizeFileName(request.File.FileName ?? "unknown"),
                request.File.Length,
                request.File.ContentType ?? "application/octet-stream",
                DateTime.UtcNow,
                request.Tags ?? new Dictionary<string, string>()
            ));
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogError(ex, "DynamoDB table not found");
            return StatusCode(500, "Database table not found. Please contact administrator.");
        }
        catch (ProvisionedThroughputExceededException ex)
        {
            _logger.LogError(ex, "DynamoDB throughput exceeded");
            return StatusCode(503, "Service temporarily unavailable. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file upload to DynamoDB");
            return StatusCode(500, "An unexpected error occurred during upload.");
        }
    }

    /// <summary>
    /// Retrieve file by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("File retrieval request received for ID: {Id}", id);

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("File ID is required.");
            }

            var fileRecord = await _fileService.GetFileAsync(id, cancellationToken);
            if (fileRecord == null)
            {
                _logger.LogWarning("File not found - Id: {Id}", id);
                return NotFound("File not found.");
            }

            _logger.LogInformation("File retrieved successfully - Id: {Id}, FileName: {FileName}",
                id, fileRecord.OriginalFileName);

            return File(fileRecord.FileContent, fileRecord.ContentType, fileRecord.OriginalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file from DynamoDB - Id: {Id}", id);
            return StatusCode(500, "An unexpected error occurred while retrieving the file.");
        }
    }

    /// <summary>
    /// Get file metadata by ID
    /// </summary>
    [HttpGet("{id}/metadata")]
    public async Task<IActionResult> GetFileMetadata(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("File metadata request received for ID: {Id}", id);

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("File ID is required.");
            }

            var fileRecord = await _fileService.GetFileAsync(id, cancellationToken);
            if (fileRecord == null)
            {
                _logger.LogWarning("File not found - Id: {Id}", id);
                return NotFound("File not found.");
            }

            return Ok(new
            {
                id = fileRecord.Id,
                originalFileName = fileRecord.OriginalFileName,
                sanitizedFileName = fileRecord.SanitizedFileName,
                contentType = fileRecord.ContentType,
                fileSize = fileRecord.FileSize,
                uploadedBy = fileRecord.UploadedBy,
                uploadedAt = fileRecord.UploadedAt,
                keyPrefix = fileRecord.KeyPrefix,
                tags = fileRecord.Tags,
                lastModified = fileRecord.LastModified,
                version = fileRecord.Version
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file metadata from DynamoDB - Id: {Id}", id);
            return StatusCode(500, "An unexpected error occurred while retrieving file metadata.");
        }
    }

    /// <summary>
    /// List all files with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListFiles(
        [FromQuery] int limit = 20,
        [FromQuery] string? lastKey = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("File listing request received - Limit: {Limit}", limit);

        try
        {
            var files = await _fileService.ListFilesAsync(limit, lastKey, cancellationToken);

            _logger.LogInformation("File listing completed - Count: {Count}", files.Count());

            return Ok(new
            {
                files = files,
                limit = limit,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files from DynamoDB");
            return StatusCode(500, "An unexpected error occurred while listing files.");
        }
    }

    /// <summary>
    /// Delete file (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("File deletion request received for ID: {Id}", id);

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("File ID is required.");
            }

            var deleted = await _fileService.DeleteFileAsync(id, cancellationToken);
            if (!deleted)
            {
                _logger.LogWarning("File not found for deletion - Id: {Id}", id);
                return NotFound("File not found.");
            }

            _logger.LogInformation("File deleted successfully - Id: {Id}", id);
            return Ok(new { message = "File deleted successfully", id = id, deletedAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from DynamoDB - Id: {Id}", id);
            return StatusCode(500, "An unexpected error occurred while deleting the file.");
        }
    }

    /// <summary>
    /// Update file tags
    /// </summary>
    [HttpPut("{id}/tags")]
    public async Task<IActionResult> UpdateFileTags(string id, [FromBody] Dictionary<string, string> tags, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("File tags update request received for ID: {Id}", id);

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("File ID is required.");
            }

            if (tags == null || tags.Count == 0)
            {
                return BadRequest("Tags are required.");
            }

            var updated = await _fileService.UpdateFileAsync(id, tags, cancellationToken);
            if (!updated)
            {
                _logger.LogWarning("File not found for update - Id: {Id}", id);
                return NotFound("File not found.");
            }

            _logger.LogInformation("File tags updated successfully - Id: {Id}", id);
            return Ok(new { message = "File tags updated successfully", id = id, updatedAt = DateTime.UtcNow, tags = tags });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file tags in DynamoDB - Id: {Id}", id);
            return StatusCode(500, "An unexpected error occurred while updating file tags.");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return $"file_{Guid.NewGuid():N}";

        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat(new[] { '/', '\\' })
            .Concat(new[] { '<', '>', '|', ':', '*', '?' })
            .ToArray();

        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Length > 255)
        {
            var extension = Path.GetExtension(fileName);
            sanitized = $"file_{Guid.NewGuid():N}{extension}";
        }

        return sanitized;
    }
}