# Configuration-Based DynamoDB Table Management

## Overview
Your application now supports **configuration-based table names** for DynamoDB, allowing you to use different tables across different environments or deployments.

## How It Works

### 1. Configuration Reading
The `DynamoFileService` now reads the table name from configuration:
```csharp
_tableName = _configuration["DynamoDB:TableName"] ?? "FileUploads";
```

### 2. Environment-Specific Configuration

#### Production (`appsettings.json`):
```json
{
  "DynamoDB": {
    "TableName": "FileUploads",
    "Region": "us-east-2"
  }
}
```

#### Development (`appsettings.Development.json`):
```json
{
  "DynamoDB": {
    "TableName": "FileUploads-Dev",
    "Region": "us-east-2"
  }
}
```

### 3. Terraform Infrastructure Support

Your Terraform configuration supports this through the `dynamodb_table_name` variable:

```hcl
variable "dynamodb_table_name" {
  type        = string
  description = "DynamoDB table name for file uploads"
  default     = "FileUploads"
}
```

## Usage Scenarios

### A. Different Environments
1. **Development**: `FileUploads-Dev`
2. **Staging**: `FileUploads-Stage`  
3. **Production**: `FileUploads`

### B. Multi-Tenant Applications
1. **Tenant A**: `FileUploads-TenantA`
2. **Tenant B**: `FileUploads-TenantB`

### C. Feature Testing
1. **Main Branch**: `FileUploads`
2. **Feature Branch**: `FileUploads-Feature-XYZ`

## Deployment Options

### Option 1: Environment Variables
```bash
# Override in Docker/ECS
DYNAMODB__TABLENAME=FileUploads-Prod
```

### Option 2: Terraform Deployment
```bash
# Deploy with custom table name
terraform apply -var="dynamodb_table_name=FileUploads-MyApp"
```

### Option 3: appsettings Override
Create environment-specific `appsettings.{Environment}.json` files with different table names.

## AWS Console Navigation

To find your files in AWS Console:
1. Go to **AWS Console** → **DynamoDB**
2. Select your region (us-east-2)
3. Click **Tables** in left sidebar
4. Click your table name (e.g., "FileUploads" or "FileUploads-Dev")
5. Click **Explore table items** to see your uploaded files

Direct link format:
```
https://us-east-2.console.aws.amazon.com/dynamodbv2/home?region=us-east-2#table?name=YOUR_TABLE_NAME
```

## Configuration Flexibility Features

✅ **Environment-specific table names**  
✅ **Runtime configuration override**  
✅ **Terraform variable support**  
✅ **Docker environment variable support**  
✅ **Fallback to default table name**  
✅ **No code changes required for different environments**

## Current File Locations

Based on your current setup:
- **Table Name**: FileUploads (or FileUploads-Dev in development)
- **Region**: us-east-2
- **Files**: 3 files currently stored
  - Perficient.docx (155 KB)
  - Terraform_Plan_Lambda.txt (9.8 KB, 2 files)

## Best Practices

1. **Use environment suffixes** (`-Dev`, `-Stage`, `-Prod`)
2. **Keep table names consistent** with your naming convention
3. **Document table purposes** in Terraform comments
4. **Use least-privilege IAM policies** per environment
5. **Monitor costs** across different table configurations