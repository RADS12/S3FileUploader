# DynamoDB File Upload Implementation Guide

## Overview

This guide shows how to upload and manage files using **Amazon DynamoDB** instead of S3. The implementation stores both file metadata and the actual file content directly in DynamoDB.

## Architecture

- **Storage**: Amazon DynamoDB (files stored as binary data)
- **API Framework**: .NET 9.0 Web API
- **Hosting**: AWS ECS Fargate
- **Security**: IAM roles, input validation, file sanitization

## ⚠️ Important Considerations

### DynamoDB Limitations

- **Item Size Limit**: 400KB per item (our implementation uses 10MB limit for demo)
- **Best for**: Small files, metadata, configuration files
- **Not Recommended for**: Large media files, documents > 10MB
- **Cost**: More expensive than S3 for large files

### When to Use DynamoDB vs S3

| Use Case         | DynamoDB               | S3                |
| ---------------- | ---------------------- | ----------------- |
| File Size        | < 10MB                 | Any size          |
| Access Pattern   | Frequent reads         | Infrequent access |
| Metadata Queries | Complex queries        | Simple key-based  |
| Cost             | Higher for large files | Lower for storage |

## API Endpoints

### 1. Upload File to DynamoDB

```http
POST /api/dynamofile/upload
Content-Type: multipart/form-data

FormData:
- File: [file]
- KeyPrefix: [optional prefix]
- Tags: [optional JSON object]
```

**Response:**

```json
{
  "id": "a1b2c3d4e5f6g7h8",
  "originalFileName": "document.pdf",
  "sanitizedFileName": "document.pdf",
  "fileSize": 1024000,
  "contentType": "application/pdf",
  "uploadedAt": "2025-09-28T10:30:00Z",
  "tags": {}
}
```

### 2. Get File by ID

```http
GET /api/dynamofile/{id}
```

Returns the actual file content with proper headers.

### 3. Get File Metadata

```http
GET /api/dynamofile/{id}/metadata
```

**Response:**

```json
{
  "id": "a1b2c3d4e5f6g7h8",
  "originalFileName": "document.pdf",
  "sanitizedFileName": "document.pdf",
  "contentType": "application/pdf",
  "fileSize": 1024000,
  "uploadedBy": "System",
  "uploadedAt": "2025-09-28T10:30:00Z",
  "keyPrefix": null,
  "tags": {},
  "lastModified": "2025-09-28T10:30:00Z",
  "version": 1
}
```

### 4. List All Files

```http
GET /api/dynamofile?limit=20&lastKey=previousId
```

**Response:**

```json
{
  "files": [
    {
      "id": "a1b2c3d4e5f6g7h8",
      "originalFileName": "document.pdf",
      "sanitizedFileName": "document.pdf",
      "fileSize": 1024000,
      "contentType": "application/pdf",
      "uploadedAt": "2025-09-28T10:30:00Z",
      "uploadedBy": "System",
      "isActive": true
    }
  ],
  "limit": 20,
  "timestamp": "2025-09-28T10:35:00Z"
}
```

### 5. Delete File (Soft Delete)

```http
DELETE /api/dynamofile/{id}
```

### 6. Update File Tags

```http
PUT /api/dynamofile/{id}/tags
Content-Type: application/json

{
  "category": "documents",
  "department": "finance",
  "confidential": "true"
}
```

### 7. Health Check

```http
GET /api/dynamofile/health
```

## DynamoDB Table Structure

### Table Name: `FileUploads`

| Attribute           | Type    | Description                          |
| ------------------- | ------- | ------------------------------------ |
| `Id` (PK)           | String  | Unique file identifier (GUID)        |
| `OriginalFileName`  | String  | Original uploaded filename           |
| `SanitizedFileName` | String  | Sanitized filename for security      |
| `ContentType`       | String  | MIME type of the file                |
| `FileSize`          | Number  | File size in bytes                   |
| `FileContent`       | Binary  | Actual file content (Base64 encoded) |
| `UploadedBy`        | String  | User who uploaded the file           |
| `UploadedAt`        | String  | Upload timestamp (ISO 8601)          |
| `KeyPrefix`         | String  | Optional prefix for organization     |
| `Tags`              | Map     | Key-value pairs for metadata         |
| `IsActive`          | Boolean | Soft delete flag                     |
| `LastModified`      | String  | Last modification timestamp          |
| `Version`           | Number  | Version number for updates           |

## AWS Setup

### 1. Create DynamoDB Table

```bash
# Using AWS CLI
aws dynamodb create-table \
    --table-name FileUploads \
    --attribute-definitions \
        AttributeName=Id,AttributeType=S \
    --key-schema \
        AttributeName=Id,KeyType=HASH \
    --provisioned-throughput \
        ReadCapacityUnits=5,WriteCapacityUnits=5 \
    --region us-east-2
```

Or use the AWS Console:

1. Go to DynamoDB → Tables → Create Table
2. Table name: `FileUploads`
3. Partition key: `Id` (String)
4. Use default settings for other options

### 2. IAM Permissions

Your ECS task role needs these DynamoDB permissions:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "dynamodb:PutItem",
        "dynamodb:GetItem",
        "dynamodb:UpdateItem",
        "dynamodb:DeleteItem",
        "dynamodb:Scan",
        "dynamodb:Query"
      ],
      "Resource": ["arn:aws:dynamodb:us-east-2:*:table/FileUploads"]
    }
  ]
}
```

## Testing the API

### 1. Upload a File

```bash
# Upload using curl
curl -X POST "http://localhost:8080/api/dynamofile/upload" \
  -H "Content-Type: multipart/form-data" \
  -F "File=@document.pdf" \
  -F "KeyPrefix=documents/2025" \
  -F "Tags={\"category\":\"finance\"}"
```

### 2. Retrieve File

```bash
# Get the file content
curl -X GET "http://localhost:8080/api/dynamofile/{file-id}" \
  --output downloaded-file.pdf

# Get file metadata
curl -X GET "http://localhost:8080/api/dynamofile/{file-id}/metadata"
```

### 3. List Files

```bash
curl -X GET "http://localhost:8080/api/dynamofile?limit=10"
```

## Configuration

### appsettings.json

```json
{
  "AWS": {
    "Region": "us-east-2"
  },
  "Upload": {
    "DefaultUploader": "System"
  },
  "FileUpload": {
    "MaxSizeBytes": "10485760"
  }
}
```

## Security Features

### Input Validation

- File size limits (10MB max)
- Content type whitelist
- Filename sanitization
- Request rate limiting

### Security Measures

- File name injection protection
- Path traversal prevention
- Content type validation
- Structured error handling

## Deployment

### 1. Update Terraform (if using)

Add DynamoDB table to your Terraform configuration:

```hcl
resource "aws_dynamodb_table" "file_uploads" {
  name           = "FileUploads"
  billing_mode   = "PROVISIONED"
  read_capacity  = 5
  write_capacity = 5
  hash_key       = "Id"

  attribute {
    name = "Id"
    type = "S"
  }

  tags = var.tags
}
```

### 2. Update IAM Role

Add DynamoDB permissions to your ECS task role.

### 3. Deploy with Docker

The existing Docker setup works with these additions:

```dockerfile
# No changes needed to Dockerfile
# DynamoDB SDK is included via NuGet package
```

## Monitoring and Logging

### CloudWatch Metrics

- DynamoDB read/write capacity utilization
- API response times
- Error rates

### Application Logs

- File upload/download operations
- Validation failures
- DynamoDB exceptions

## Cost Optimization

### DynamoDB Pricing Considerations

- **On-Demand**: Pay per request (good for variable traffic)
- **Provisioned**: Fixed capacity (cheaper for consistent traffic)
- **Storage**: $0.25 per GB per month

### Recommendations

- Use S3 for files > 10MB
- Consider DynamoDB only for frequently accessed small files
- Monitor costs via AWS Cost Explorer

## Comparison: DynamoDB vs S3

| Feature         | DynamoDB Implementation                | S3 Implementation              |
| --------------- | -------------------------------------- | ------------------------------ |
| **Setup**       | ✅ Simple table creation               | ✅ Simple bucket creation      |
| **File Size**   | ⚠️ Limited to 10MB (400KB recommended) | ✅ Unlimited                   |
| **Performance** | ✅ Very fast reads/writes              | ✅ Good for large files        |
| **Cost**        | ❌ Expensive for large files           | ✅ Cost-effective storage      |
| **Querying**    | ✅ Rich querying capabilities          | ⚠️ Limited to key-based access |
| **Metadata**    | ✅ Native JSON support                 | ⚠️ Limited metadata options    |
| **Backup**      | ✅ Point-in-time recovery              | ✅ Versioning & replication    |
| **Use Case**    | Configuration, small docs              | Media, backups, archives       |

## Conclusion

The DynamoDB implementation is excellent for:

- ✅ Small files (< 10MB)
- ✅ Frequently accessed files
- ✅ Rich metadata requirements
- ✅ Complex querying needs

Use S3 implementation for:

- ✅ Large files
- ✅ Infrequent access patterns
- ✅ Cost-sensitive applications
- ✅ Long-term archival

---

_Choose the implementation based on your specific use case, file sizes, and access patterns._
