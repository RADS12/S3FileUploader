using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using FileUploaderApi.Models;

namespace FileUploaderApi.Services;

/// <summary>
/// Service for handling DynamoDB file operations
/// </summary>
public interface IDynamoFileService
{
    Task<string> UploadFileAsync(DynamoUploadRequest request, CancellationToken cancellationToken = default);
    Task<FileUploadRecord?> GetFileAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<FileListResponse>> ListFilesAsync(int limit = 50, string? lastEvaluatedKey = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> UpdateFileAsync(string id, Dictionary<string, string> tags, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string id, CancellationToken cancellationToken = default);
}

public class DynamoFileService : IDynamoFileService
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly IDynamoDBContext _dynamoContext;
    private readonly ILogger<DynamoFileService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _tableName;

    public DynamoFileService(
        IAmazonDynamoDB dynamoDb,
        IDynamoDBContext dynamoContext,
        ILogger<DynamoFileService> logger,
        IConfiguration configuration)
    {
        _dynamoDb = dynamoDb;
        _dynamoContext = dynamoContext;
        _logger = logger;
        _configuration = configuration;
        _tableName = _configuration["DynamoDB:TableName"] ?? "FileUploads";
    }

    public async Task<string> UploadFileAsync(DynamoUploadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var id = Guid.NewGuid().ToString("N");

            // Read file content into memory
            await using var stream = request.File.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            var fileContent = memoryStream.ToArray();

            var uploadRecord = new FileUploadRecord
            {
                Id = id,
                OriginalFileName = request.File.FileName ?? "unknown",
                SanitizedFileName = SanitizeFileName(request.File.FileName ?? "unknown"),
                ContentType = request.File.ContentType ?? "application/octet-stream",
                FileSize = request.File.Length,
                FileContent = fileContent,
                UploadedBy = _configuration["Upload:DefaultUploader"] ?? "System",
                UploadedAt = DateTime.UtcNow,
                KeyPrefix = request.KeyPrefix,
                Tags = request.Tags ?? new Dictionary<string, string>(),
                LastModified = DateTime.UtcNow
            };

            await _dynamoContext.SaveAsync(uploadRecord, cancellationToken);

            _logger.LogInformation("File uploaded to DynamoDB - Id: {Id}, FileName: {FileName}, Size: {Size} bytes",
                id, uploadRecord.OriginalFileName, uploadRecord.FileSize);

            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to DynamoDB");
            throw;
        }
    }

    public async Task<FileUploadRecord?> GetFileAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await _dynamoContext.LoadAsync<FileUploadRecord>(id, cancellationToken);

            if (record != null && record.IsActive)
            {
                _logger.LogInformation("File retrieved from DynamoDB - Id: {Id}, FileName: {FileName}",
                    id, record.OriginalFileName);
                return record;
            }

            _logger.LogWarning("File not found or inactive in DynamoDB - Id: {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file from DynamoDB - Id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<FileListResponse>> ListFilesAsync(int limit = 50, string? lastEvaluatedKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var scanRequest = new ScanRequest
            {
                TableName = _tableName,
                Limit = Math.Min(limit, 100), // Limit to max 100 items
                FilterExpression = "IsActive = :isActive",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":isActive", new AttributeValue { N = "1" } }
                }
            };

            if (!string.IsNullOrWhiteSpace(lastEvaluatedKey))
            {
                scanRequest.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = lastEvaluatedKey } }
                };
            }

            var response = await _dynamoDb.ScanAsync(scanRequest, cancellationToken);

            var files = response.Items.Select(item => new FileListResponse(
                item["Id"].S,
                item.ContainsKey("OriginalFileName") ? item["OriginalFileName"].S : "unknown",
                item.ContainsKey("SanitizedFileName") ? item["SanitizedFileName"].S : "unknown",
                item.ContainsKey("FileSize") ? long.Parse(item["FileSize"].N) : 0,
                item.ContainsKey("ContentType") ? item["ContentType"].S : "application/octet-stream",
                item.ContainsKey("UploadedAt") ? DateTime.Parse(item["UploadedAt"].S) : DateTime.MinValue,
                item.ContainsKey("UploadedBy") ? item["UploadedBy"].S : "unknown",
                item.ContainsKey("IsActive") ? (item["IsActive"].N == "1") : true
            )).ToList();

            _logger.LogInformation("Listed {Count} files from DynamoDB", files.Count);
            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files from DynamoDB");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Soft delete - mark as inactive
            var record = await _dynamoContext.LoadAsync<FileUploadRecord>(id, cancellationToken);
            if (record != null)
            {
                record.IsActive = false;
                record.LastModified = DateTime.UtcNow;
                await _dynamoContext.SaveAsync(record, cancellationToken);

                _logger.LogInformation("File soft-deleted in DynamoDB - Id: {Id}", id);
                return true;
            }

            _logger.LogWarning("File not found for deletion in DynamoDB - Id: {Id}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file in DynamoDB - Id: {Id}", id);
            throw;
        }
    }

    public async Task<bool> UpdateFileAsync(string id, Dictionary<string, string> tags, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await _dynamoContext.LoadAsync<FileUploadRecord>(id, cancellationToken);
            if (record != null && record.IsActive)
            {
                record.Tags = tags;
                record.LastModified = DateTime.UtcNow;
                record.Version++;

                await _dynamoContext.SaveAsync(record, cancellationToken);

                _logger.LogInformation("File updated in DynamoDB - Id: {Id}", id);
                return true;
            }

            _logger.LogWarning("File not found for update in DynamoDB - Id: {Id}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file in DynamoDB - Id: {Id}", id);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await _dynamoContext.LoadAsync<FileUploadRecord>(id, cancellationToken);
            return record != null && record.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence in DynamoDB - Id: {Id}", id);
            return false;
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