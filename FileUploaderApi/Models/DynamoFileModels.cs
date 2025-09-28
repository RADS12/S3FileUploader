using Amazon.DynamoDBv2.DataModel;

namespace FileUploaderApi.Models;

/// <summary>
/// DynamoDB model for storing file metadata
/// </summary>
[DynamoDBTable("FileUploads")]
public class FileUploadRecord
{
    [DynamoDBHashKey("Id")]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty("OriginalFileName")]
    public string OriginalFileName { get; set; } = string.Empty;

    [DynamoDBProperty("SanitizedFileName")]
    public string SanitizedFileName { get; set; } = string.Empty;

    [DynamoDBProperty("ContentType")]
    public string ContentType { get; set; } = string.Empty;

    [DynamoDBProperty("FileSize")]
    public long FileSize { get; set; }

    [DynamoDBProperty("FileContent")]
    public byte[] FileContent { get; set; } = Array.Empty<byte>();

    [DynamoDBProperty("UploadedBy")]
    public string UploadedBy { get; set; } = string.Empty;

    [DynamoDBProperty("UploadedAt")]
    public DateTime UploadedAt { get; set; }

    [DynamoDBProperty("KeyPrefix")]
    public string? KeyPrefix { get; set; }

    [DynamoDBProperty("Tags")]
    public Dictionary<string, string> Tags { get; set; } = new();

    [DynamoDBProperty("IsActive")]
    public bool IsActive { get; set; } = true;

    [DynamoDBProperty("LastModified")]
    public DateTime LastModified { get; set; }

    [DynamoDBProperty("Version")]
    public int Version { get; set; } = 1;
}

/// <summary>
/// Request model for DynamoDB file upload
/// </summary>
public class DynamoUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public string? KeyPrefix { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
}

/// <summary>
/// Response model for DynamoDB file upload
/// </summary>
public record DynamoUploadResponse(
    string Id,
    string OriginalFileName,
    string SanitizedFileName,
    long FileSize,
    string ContentType,
    DateTime UploadedAt,
    Dictionary<string, string> Tags
);

/// <summary>
/// Response model for file retrieval
/// </summary>
public record FileRetrievalResponse(
    string Id,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    byte[] FileContent,
    DateTime UploadedAt
);

/// <summary>
/// Response model for file listing
/// </summary>
public record FileListResponse(
    string Id,
    string OriginalFileName,
    string SanitizedFileName,
    long FileSize,
    string ContentType,
    DateTime UploadedAt,
    string UploadedBy,
    bool IsActive
);