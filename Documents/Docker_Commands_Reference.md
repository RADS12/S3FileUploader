# Docker Commands Reference - Configuration-Based Approach

## 🐳 Overview

Your application now supports **configuration-based DynamoDB table management**. This means you can use different table names for different environments without changing code.

## 📋 Quick Reference

### Build Commands
```bash
# Build the Docker image
docker build -t fileuploaderapi:latest .

# Build with specific tag
docker build -t fileuploaderapi:dev .
```

### Environment-Based Deployment

#### 🔵 Development Environment
Uses `FileUploads-Dev` table (from `appsettings.Development.json`)

```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest
```

#### 🟢 Production Environment  
Uses `FileUploads` table (from `appsettings.json`)

```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest
```

#### 🟡 Custom Table Name
Override table name regardless of environment

```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e DYNAMODB__TABLENAME=FileUploads-MyCustom \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest
```

## 🔧 Advanced Configuration

### Multi-Environment Deployment

#### Feature Testing
```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e DYNAMODB__TABLENAME=FileUploads-Feature-Auth \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest
```

#### Staging Environment
```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Staging \
  -e DYNAMODB__TABLENAME=FileUploads-Stage \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest
```

### Volume Mount for AWS Credentials

#### Windows
```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e DYNAMODB__TABLENAME=FileUploads-Dev \
  -v C:\Users\%USERNAME%\.aws:/root/.aws:ro \
  fileuploaderapi:latest
```

#### Linux/MacOS
```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e DYNAMODB__TABLENAME=FileUploads-Dev \
  -v ~/.aws:/root/.aws:ro \
  fileuploaderapi:latest
```

### Complete Environment Variables

```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e DYNAMODB__TABLENAME=FileUploads-Dev \
  -e DYNAMODB__REGION=us-east-2 \
  -e DYNAMODB__LOCALMODE=false \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest
```

## 🧪 Testing Commands

### Health Check
```bash
# Test application health
curl http://localhost:8080/health

# Test DynamoDB connectivity
curl http://localhost:8080/api/DynamoFile/health
```

### API Testing

#### DynamoDB Endpoints
```bash
# Upload file
curl -F "file=@./test.pdf" \
  -F "uploadedBy=testuser" \
  http://localhost:8080/api/DynamoFile/upload

# List files
curl http://localhost:8080/api/DynamoFile

# Download file by ID
curl -o downloaded.pdf http://localhost:8080/api/DynamoFile/download/[FILE_ID]

# Delete file by ID
curl -X DELETE http://localhost:8080/api/DynamoFile/[FILE_ID]
```

#### S3 Endpoints (still available)
```bash
# Upload to S3
curl -F "file=@./test.pdf" http://localhost:8080/api/fileupload/upload

# Get S3 download URL
curl http://localhost:8080/api/fileupload/download-url/[S3_KEY]
```

## 🛠️ Container Management

### Start/Stop/Restart

```bash
# List running containers
docker ps

# Stop all containers
docker ps -q | ForEach-Object { docker stop $_ }

# Remove all containers
docker ps -aq | ForEach-Object { docker rm $_ }

# Complete restart workflow
docker ps -q | ForEach-Object { docker stop $_ }
docker build -t fileuploaderapi:latest .
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest
```

### Container Logs and Debugging

```bash
# View container logs (get container ID first)
docker ps
docker logs [CONTAINER_ID]

# Follow logs in real-time
docker logs -f [CONTAINER_ID]

# Execute shell inside running container
docker exec -it [CONTAINER_ID] /bin/bash
```

## 🌐 Environment Configuration Matrix

| Environment | Table Name | Config File | Environment Variable Override |
|------------|------------|-------------|------------------------------|
| **Development** | `FileUploads-Dev` | `appsettings.Development.json` | `DYNAMODB__TABLENAME=FileUploads-Dev` |
| **Staging** | `FileUploads-Stage` | `appsettings.Staging.json` | `DYNAMODB__TABLENAME=FileUploads-Stage` |
| **Production** | `FileUploads` | `appsettings.json` | `DYNAMODB__TABLENAME=FileUploads` |
| **Custom** | Any name | Any file | `DYNAMODB__TABLENAME=YourTableName` |

## 🔍 Troubleshooting

### Common Issues

#### 1. AWS Credentials Not Found
```
Error: Failed to resolve AWS credentials
```

**Solutions:**
```bash
# Verify AWS CLI is configured
aws configure list

# Test AWS access
aws dynamodb list-tables --region us-east-2

# Use explicit credentials in Docker
docker run -p 8080:8080 \
  -e AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE \
  -e AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest
```

#### 2. DynamoDB Table Not Found
```
Error: ResourceNotFoundException: Requested resource not found
```

**Solutions:**
```bash
# Check if table exists
aws dynamodb describe-table --table-name FileUploads --region us-east-2

# Deploy table with Terraform
cd Infrastructure
terraform apply -var="dynamodb_table_name=FileUploads"

# Override table name in Docker
docker run -p 8080:8080 -e DYNAMODB__TABLENAME=YourExistingTable ...
```

#### 3. Port Already in Use
```
Error: bind: address already in use
```

**Solutions:**
```bash
# Use different port
docker run -p 8081:8080 ...

# Stop conflicting containers
docker ps
docker stop [CONTAINER_ID]
```

#### 4. Configuration Not Loading
```
Using fallback table name: FileUploads
```

**Solutions:**
```bash
# Check environment variable format (double underscore)
-e DYNAMODB__TABLENAME=YourTable

# Verify appsettings.json content
docker exec -it [CONTAINER_ID] cat /app/appsettings.json
```

## 📊 Monitoring and Logs

### Application Health
```bash
# Health endpoint
curl http://localhost:8080/health

# DynamoDB health
curl http://localhost:8080/api/DynamoFile/health

# Swagger UI
# Open: http://localhost:8080/swagger/index.html
```

### Container Resources
```bash
# Monitor resource usage
docker stats

# Check container details
docker inspect [CONTAINER_ID]
```

### Log Analysis
```bash
# Search for specific errors
docker logs [CONTAINER_ID] 2>&1 | grep -i "error"

# Filter DynamoDB related logs
docker logs [CONTAINER_ID] 2>&1 | grep -i "dynamo"

# View recent logs only
docker logs --since 30m [CONTAINER_ID]
```

## 🚀 Deployment Scripts

### Quick Development Start
```bash
#!/bin/bash
# dev-start.sh
docker ps -q | xargs -r docker stop
docker build -t fileuploaderapi:dev .
docker run -d -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  --name fileuploader-dev \
  fileuploaderapi:dev

echo "Development server started at http://localhost:8080"
echo "Swagger UI: http://localhost:8080/swagger/index.html"
```

### Production Deployment
```bash
#!/bin/bash
# prod-deploy.sh
docker build -t fileuploaderapi:prod .
docker run -d -p 80:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e AWS_ACCESS_KEY_ID=$PROD_AWS_KEY \
  -e AWS_SECRET_ACCESS_KEY=$PROD_AWS_SECRET \
  -e AWS_DEFAULT_REGION=us-east-2 \
  --name fileuploader-prod \
  --restart unless-stopped \
  fileuploaderapi:prod
```

---

## 🎯 Best Practices

1. **Never hardcode AWS credentials** in Dockerfiles or images
2. **Use environment-specific table names** to avoid data mixing
3. **Set restart policies** for production containers: `--restart unless-stopped`
4. **Monitor container logs** regularly for errors
5. **Use health checks** to verify container status
6. **Tag images appropriately** for version tracking
7. **Clean up unused containers** and images regularly: `docker system prune`

---

_Last Updated: September 28, 2025_
_Configuration-Based DynamoDB Table Management Enabled_ ✅