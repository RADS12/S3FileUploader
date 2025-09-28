# File Uploader API Documentation

## Overview

The File Uploader API is a .NET 9.0 Web API that provides secure file upload and download functionality using Amazon S3 storage. The API is containerized and deployed on AWS ECS Fargate with an Application Load Balancer for high availability and scalability.

## Architecture

- **Framework**: .NET 9.0 Web API
- **Containerization**: Docker
- **Hosting**: AWS ECS Fargate
- **Load Balancer**: AWS Application Load Balancer
- **Storage**: Amazon S3
- **Image Registry**: AWS ECR
- **Infrastructure**: Terraform (Infrastructure as Code)

## Base URL

```
http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com
```

## Interactive Documentation

Access the Swagger UI for interactive API testing:

```
http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/swagger/index.html
```

## Authentication

Currently, the API uses AWS IAM roles for S3 access. The ECS tasks run with appropriate IAM permissions to read/write to the configured S3 bucket.

## Endpoints

### 1. Upload File

Upload a file to S3 storage.

**Endpoint**: `POST /api/FileUpload/upload`

**Content-Type**: `multipart/form-data`

**Request Parameters**:

- `File` (required): The file to upload
- `KeyPrefix` (optional): S3 key prefix for organizing files

**Request Example**:

```bash
curl -X POST \
  http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/api/FileUpload/upload \
  -H "Content-Type: multipart/form-data" \
  -F "File=@/path/to/your/file.pdf" \
  -F "KeyPrefix=documents/2025"
```

**Success Response** (200 OK):

```json
{
  "bucket": "rad-s3-demo-first-1",
  "key": "documents/2025/file.pdf"
}
```

**Error Response** (400 Bad Request):

```json
{
  "error": "No file provided."
}
```

**Features**:

- Automatic content type detection
- Configurable S3 key prefix for file organization
- Metadata tagging (uploaded-by: "Rad")
- Support for any file type

### 2. Get Download URL

Generate a presigned URL for downloading a file from S3.

**Endpoint**: `GET /api/FileUpload/download-url/{key}`

**Parameters**:

- `key` (path parameter, required): The S3 object key
- `minutes` (query parameter, optional): URL expiration time in minutes (default: 15, max: 60)

**Request Example**:

```bash
curl -X GET \
  "http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/api/FileUpload/download-url/documents%2F2025%2Ffile.pdf?minutes=30"
```

**Success Response** (200 OK):

```json
{
  "url": "https://rad-s3-demo-first-1.s3.amazonaws.com/documents/2025/file.pdf?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=...",
  "expiresInMinutes": 30
}
```

**Features**:

- Secure presigned URLs for temporary access
- Configurable expiration time (1-60 minutes)
- Direct S3 access without proxying through API

## Configuration

### Environment Variables

The API uses the following environment variables:

- `AWS_REGION`: AWS region (default: us-east-2)
- `S3:BucketName`: S3 bucket name for file storage
- `ASPNETCORE_URLS`: Application URLs (http://+:8080)

### File Upload Limits

- **Maximum file size**: 500 MB (configurable via `FileUpload:MaxSizeBytes`)
- **Memory buffer threshold**: Unlimited (streams large files to disk)
- **Supported formats**: All file types

## Infrastructure Details

### AWS Resources

- **ECS Cluster**: `file-uploader-api`
- **ECR Repository**: `675016865089.dkr.ecr.us-east-2.amazonaws.com/file-uploader-api`
- **S3 Bucket**: `rad-s3-demo-first-1`
- **Load Balancer**: Application Load Balancer with health checks

### Security Groups

- **ALB Security Group**: Allows inbound HTTP (port 80) from internet
- **ECS Security Group**: Allows inbound traffic (port 8080) from ALB only

### Health Checks

- **Path**: `/health` (returns 200 or 404 for healthy status)
- **Interval**: 30 seconds
- **Timeout**: 5 seconds
- **Healthy threshold**: 2 consecutive successes
- **Unhealthy threshold**: 2 consecutive failures

## Development

### Local Development

1. **Prerequisites**:

   - .NET 9.0 SDK
   - Docker Desktop
   - AWS CLI configured with appropriate credentials

2. **Run locally**:

   ```bash
   cd FileUploaderApi
   dotnet run
   ```

3. **Build Docker image**:
   ```bash
   docker build -t file-uploader-api:dev .
   ```

### Deployment

The application is deployed using Terraform:

```bash
cd Infrastructure
terraform init
terraform plan
terraform apply
```

## Monitoring and Logging

### CloudWatch Logs

Application logs are available in CloudWatch:

- **Log Group**: `/ecs/file-uploader-api`
- **Log Stream**: `ecs/file-uploader-api/{task-id}`

### Health Monitoring

- **ECS Service Health**: Monitor running task count and service status
- **Load Balancer Health**: Target group health checks
- **Application Health**: Custom health endpoint at `/health`

## Error Handling

### Common Error Codes

- **400 Bad Request**: Invalid request parameters or missing file
- **500 Internal Server Error**: AWS service errors or application exceptions
- **503 Service Unavailable**: ECS service or load balancer issues

### Troubleshooting

1. **File upload failures**:

   - Check file size limits (500 MB max)
   - Verify S3 bucket permissions
   - Check ECS task IAM role permissions

2. **Download URL generation failures**:

   - Verify the S3 object key exists
   - Check IAM permissions for S3 access
   - Ensure key is properly URL encoded

3. **Service unavailable**:
   - Check ECS service status
   - Verify load balancer target health
   - Review CloudWatch logs for application errors

## Security Considerations

### Best Practices Implemented

- **IAM Roles**: Fine-grained permissions for S3 access
- **Network Security**: Security groups restrict access between components
- **Container Security**: Non-root user execution
- **Presigned URLs**: Temporary, secure access to files
- **CORS**: Configurable for web application integration

### Recommendations for Production

- **HTTPS**: Add SSL/TLS certificate to load balancer
- **Authentication**: Implement API key or JWT token authentication
- **Rate Limiting**: Add request rate limiting
- **File Validation**: Implement file type and content validation
- **Encryption**: Enable S3 server-side encryption
- **Monitoring**: Add comprehensive monitoring and alerting

## Cost Optimization

### Current Configuration Costs

- **ECS Fargate**: 0.5 vCPU, 1 GB memory (~$15/month for continuous running)
- **Application Load Balancer**: ~$16/month + data processing
- **ECR Storage**: ~$0.10/GB/month for Docker images
- **S3 Storage**: Variable based on usage
- **Data Transfer**: Standard AWS rates

### Cost Optimization Tips

- Scale ECS service to 0 during non-business hours
- Use S3 Intelligent Tiering for long-term storage
- Monitor and set up billing alerts
- Consider AWS Free Tier benefits

## API Versioning

- **Current Version**: v1
- **Swagger Endpoint**: `/swagger/v1/swagger.json`
- **Future Versions**: Will be implemented with URL versioning (`/api/v2/...`)

## Support and Contact

For technical support or questions about this API:

- **Project Repository**: [GitHub - S3FileUploader](https://github.com/RADS12/S3FileUploader)
- **Infrastructure**: Managed via Terraform in `Infrastructure/` folder
- **API Documentation**: This document and Swagger UI

---

_Last Updated: September 27, 2025_
