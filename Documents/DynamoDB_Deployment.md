# DynamoDB Infrastructure Deployment Guide

## 🚀 Quick Start

### 1. Review Configuration

Your application now supports **configuration-based table names**! The table name is read from:

1. **Environment variable**: `DYNAMODB__TABLENAME`
2. **appsettings.json**: `DynamoDB:TableName`
3. **Fallback**: `FileUploads` (if no configuration found)

```bash
# Copy example variables file
cp terraform.tfvars.example terraform.tfvars

# Edit terraform.tfvars with your specific values
# Key settings to review:
# - dynamodb_table_name (matches your appsettings.json)
# - dynamodb_billing_mode (PROVISIONED vs PAY_PER_REQUEST)
# - dynamodb_read_capacity/write_capacity
# - region
```

### 2. Initialize Terraform

```bash
cd Infrastructure
terraform init
```

### 3. Plan Deployment

```bash
# See what will be created/modified
terraform plan
```

### 4. Deploy DynamoDB Resources

```bash
# Apply changes (will prompt for confirmation)
terraform apply

# Or auto-approve for CI/CD
terraform apply -auto-approve
```

### 5. Verify Deployment

```bash
# Check outputs
terraform output

# Verify DynamoDB table exists
aws dynamodb describe-table --table-name FileUploads --region us-east-2
```

## 📋 Terraform Commands with Expected Outputs

### 1. `terraform init` - Initialize Terraform

**Command:**

```bash
cd Infrastructure
terraform init
```

**Expected Output:**

```
Initializing the backend...

Initializing provider plugins...
- Finding hashicorp/aws versions matching "~> 5.0"...
- Installing hashicorp/aws v5.17.0...
- Installed hashicorp/aws v5.17.0 (signed by HashiCorp)

Terraform has been successfully initialized!

You may now begin working with Terraform. Try running "terraform plan" to see
any changes that are required for your infrastructure.

If you ever set or change modules or backend configuration for Terraform,
rerun this command to reinitialize your working directory.
```

### 2. `terraform validate` - Validate Configuration

**Command:**

```bash
terraform validate
```

**Expected Output:**

```
Success! The configuration is valid.
```

### 3. `terraform plan` - Preview Changes

**Command:**

```bash
terraform plan
```

**Expected Output (Sample):**

```
Terraform used the selected providers to generate the following execution plan. Resource actions are indicated with the following symbols:
  + create

Terraform will perform the following actions:

  # aws_cloudwatch_metric_alarm.dynamodb_read_throttles[0] will be created
  + resource "aws_cloudwatch_metric_alarm" "dynamodb_read_throttles" {
      + alarm_name          = "FileUploads-read-throttles"
      + comparison_operator = "GreaterThanThreshold"
      + evaluation_periods  = 2
      + metric_name         = "UserErrors"
      + namespace           = "AWS/DynamoDB"
      + period              = 300
      + statistic           = "Sum"
      + threshold           = 5
      + treat_missing_data  = "missing"
    }

  # aws_dynamodb_table.file_uploads will be created
  + resource "aws_dynamodb_table" "file_uploads" {
      + arn              = (known after apply)
      + billing_mode     = "PROVISIONED"
      + hash_key         = "Id"
      + id               = (known after apply)
      + name             = "FileUploads"
      + read_capacity    = 5
      + stream_arn       = (known after apply)
      + stream_label     = (known after apply)
      + write_capacity   = 5

      + attribute {
          + name = "ContentType"
          + type = "S"
        }
      + attribute {
          + name = "Id"
          + type = "S"
        }
      + attribute {
          + name = "UploadedAt"
          + type = "S"
        }
      + attribute {
          + name = "UploadedBy"
          + type = "S"
        }

      + global_secondary_index {
          + hash_key           = "ContentType"
          + name               = "ContentTypeIndex"
          + projection_type    = "KEYS_ONLY"
          + range_key          = "UploadedAt"
          + read_capacity      = 2
          + write_capacity     = 2
        }
      + global_secondary_index {
          + hash_key           = "UploadedBy"
          + name               = "UploadedAtIndex"
          + projection_type    = "ALL"
          + range_key          = "UploadedAt"
          + read_capacity      = 2
          + write_capacity     = 2
        }

      + point_in_time_recovery {
          + enabled = true
        }

      + server_side_encryption {
          + enabled = true
        }
    }

  # aws_iam_policy.dynamodb_rw will be created
  + resource "aws_iam_policy" "dynamodb_rw" {
      + arn         = (known after apply)
      + description = (known after apply)
      + id          = (known after apply)
      + name        = "AppRunnerDynamoDBRW-file-uploader-api"
      + policy      = (known after apply)
    }

Plan: 15 to add, 1 to change, 0 to destroy.

Changes to Outputs:
  + dynamodb_billing_mode = "PROVISIONED"
  + dynamodb_gsi_names    = [
      + "UploadedAtIndex",
      + "ContentTypeIndex",
    ]
  + dynamodb_region       = "us-east-2"
  + dynamodb_table_arn    = (known after apply)
  + dynamodb_table_name   = "FileUploads"
```

### 4. `terraform apply` - Deploy Infrastructure

**Command:**

```bash
terraform apply
```

**Interactive Prompt:**

```
Do you want to perform these actions?
  Terraform will perform the actions described above.
  Only 'yes' will be accepted to approve.

  Enter a value: yes
```

**Expected Output (Sample):**

```
aws_iam_policy_document.dynamodb_rw: Reading...
aws_iam_policy_document.dynamodb_rw: Read complete after 0s
aws_iam_policy.dynamodb_rw: Creating...
aws_dynamodb_table.file_uploads: Creating...
aws_iam_policy.dynamodb_rw: Creation complete after 1s [id=arn:aws:iam::123456789012:policy/AppRunnerDynamoDBRW-file-uploader-api]
aws_iam_role_policy_attachment.apprunner_dynamodb_attach: Creating...
aws_iam_role_policy_attachment.apprunner_dynamodb_attach: Creation complete after 1s
aws_dynamodb_table.file_uploads: Still creating... [10s elapsed]
aws_dynamodb_table.file_uploads: Still creating... [20s elapsed]
aws_dynamodb_table.file_uploads: Creation complete after 25s [id=FileUploads]
aws_cloudwatch_metric_alarm.dynamodb_read_throttles[0]: Creating...
aws_cloudwatch_metric_alarm.dynamodb_write_throttles[0]: Creating...
aws_cloudwatch_metric_alarm.dynamodb_high_read_capacity[0]: Creating...
aws_cloudwatch_metric_alarm.dynamodb_high_write_capacity[0]: Creating...
aws_cloudwatch_metric_alarm.dynamodb_read_throttles[0]: Creation complete after 1s
aws_cloudwatch_metric_alarm.dynamodb_write_throttles[0]: Creation complete after 1s
aws_cloudwatch_metric_alarm.dynamodb_high_read_capacity[0]: Creation complete after 1s
aws_cloudwatch_metric_alarm.dynamodb_high_write_capacity[0]: Creation complete after 1s

Apply complete! Resources: 15 added, 1 changed, 0 destroyed.

Outputs:

bucket_in_use = "rad-s3-demo-first-1"
dynamodb_billing_mode = "PROVISIONED"
dynamodb_gsi_names = toset([
  "ContentTypeIndex",
  "UploadedAtIndex",
])
dynamodb_region = "us-east-2"
dynamodb_table_arn = "arn:aws:dynamodb:us-east-2:123456789012:table/FileUploads"
dynamodb_table_name = "FileUploads"
ecr_repository = "123456789012.dkr.ecr.us-east-2.amazonaws.com/file-uploader-api"
ecs_cluster = "file-uploader-api"
image_tag_used = "dev"
load_balancer_dns = "file-uploader-api-1234567890.us-east-2.elb.amazonaws.com"
service_url = "http://file-uploader-api-1234567890.us-east-2.elb.amazonaws.com"
```

### 5. `terraform output` - Show Deployment Results

**Command:**

```bash
terraform output
```

**Expected Output:**

```
bucket_in_use = "rad-s3-demo-first-1"
dynamodb_billing_mode = "PROVISIONED"
dynamodb_gsi_names = toset([
  "ContentTypeIndex",
  "UploadedAtIndex",
])
dynamodb_region = "us-east-2"
dynamodb_table_arn = "arn:aws:dynamodb:us-east-2:123456789012:table/FileUploads"
dynamodb_table_name = "FileUploads"
ecr_repository = "123456789012.dkr.ecr.us-east-2.amazonaws.com/file-uploader-api"
ecs_cluster = "file-uploader-api"
image_tag_used = "dev"
load_balancer_dns = "file-uploader-api-1234567890.us-east-2.elb.amazonaws.com"
service_url = "http://file-uploader-api-1234567890.us-east-2.elb.amazonaws.com"
```

### 6. `terraform show` - Show Current State

**Command:**

```bash
terraform show
```

**Expected Output (Sample - truncated):**

```
# aws_dynamodb_table.file_uploads:
resource "aws_dynamodb_table" "file_uploads" {
    arn              = "arn:aws:dynamodb:us-east-2:123456789012:table/FileUploads"
    billing_mode     = "PROVISIONED"
    deletion_protection_enabled = true
    hash_key         = "Id"
    id               = "FileUploads"
    name             = "FileUploads"
    read_capacity    = 5
    stream_enabled   = false
    table_class      = "STANDARD"
    tags             = {
        "Description" = "DynamoDB table for file upload storage and metadata"
        "Env"         = "dev"
        "Name"        = "FileUploads"
        "Owner"       = "rad"
        "Project"     = "file-uploader-api"
        "Service"     = "FileUploader"
    }
    tags_all         = {
        "Description" = "DynamoDB table for file upload storage and metadata"
        "Env"         = "dev"
        "Name"        = "FileUploads"
        "Owner"       = "rad"
        "Project"     = "file-uploader-api"
        "Service"     = "FileUploader"
    }
    write_capacity   = 5

    attribute {
        name = "ContentType"
        type = "S"
    }
    attribute {
        name = "Id"
        type = "S"
    }
    attribute {
        name = "UploadedAt"
        type = "S"
    }
    attribute {
        name = "UploadedBy"
        type = "S"
    }

    global_secondary_index {
        hash_key           = "ContentType"
        name               = "ContentTypeIndex"
        non_key_attributes = []
        projection_type    = "KEYS_ONLY"
        range_key          = "UploadedAt"
        read_capacity      = 2
        write_capacity     = 2
    }
    global_secondary_index {
        hash_key           = "UploadedBy"
        name               = "UploadedAtIndex"
        non_key_attributes = []
        projection_type    = "ALL"
        range_key          = "UploadedAt"
        read_capacity      = 2
        write_capacity     = 2
    }

    point_in_time_recovery {
        enabled = true
    }

    server_side_encryption {
        enabled = true
    }
}
```

### 7. `terraform state list` - List All Resources

**Command:**

```bash
terraform state list
```

**Expected Output:**

```
aws_cloudwatch_metric_alarm.dynamodb_high_read_capacity[0]
aws_cloudwatch_metric_alarm.dynamodb_high_write_capacity[0]
aws_cloudwatch_metric_alarm.dynamodb_read_throttles[0]
aws_cloudwatch_metric_alarm.dynamodb_write_throttles[0]
aws_dynamodb_table.file_uploads
aws_ecr_repository.api
aws_ecs_cluster.api
aws_ecs_service.api
aws_ecs_task_definition.api
aws_iam_policy.dynamodb_rw
aws_iam_policy.s3_rw
aws_iam_role.apprunner_access_role
aws_iam_role.apprunner_instance_role
aws_iam_role.ecs_execution_role
aws_iam_role_policy_attachment.apprunner_access_attach
aws_iam_role_policy_attachment.apprunner_dynamodb_attach
aws_iam_role_policy_attachment.apprunner_instance_attach
aws_iam_role_policy_attachment.ecs_execution_attach
aws_lb.api
aws_lb_listener.api
aws_lb_target_group.api
aws_security_group.alb
aws_security_group.ecs
null_resource.build_and_push
```

### 8. Verification Commands with Outputs

**AWS CLI Verification:**

```bash
# Check DynamoDB table details
aws dynamodb describe-table --table-name FileUploads --region us-east-2
```

**Expected Output:**

```json
{
  "Table": {
    "AttributeDefinitions": [
      {
        "AttributeName": "Id",
        "AttributeType": "S"
      },
      {
        "AttributeName": "UploadedBy",
        "AttributeType": "S"
      },
      {
        "AttributeName": "UploadedAt",
        "AttributeType": "S"
      },
      {
        "AttributeName": "ContentType",
        "AttributeType": "S"
      }
    ],
    "TableName": "FileUploads",
    "KeySchema": [
      {
        "AttributeName": "Id",
        "KeyType": "HASH"
      }
    ],
    "TableStatus": "ACTIVE",
    "CreationDateTime": "2025-09-28T10:30:00.000000+00:00",
    "ProvisionedThroughput": {
      "NumberOfDecreasesToday": 0,
      "ReadCapacityUnits": 5,
      "WriteCapacityUnits": 5
    },
    "TableSizeBytes": 0,
    "ItemCount": 0,
    "TableArn": "arn:aws:dynamodb:us-east-2:123456789012:table/FileUploads",
    "GlobalSecondaryIndexes": [
      {
        "IndexName": "UploadedAtIndex",
        "KeySchema": [
          {
            "AttributeName": "UploadedBy",
            "KeyType": "HASH"
          },
          {
            "AttributeName": "UploadedAt",
            "KeyType": "RANGE"
          }
        ],
        "Projection": {
          "ProjectionType": "ALL"
        },
        "IndexStatus": "ACTIVE",
        "ProvisionedThroughput": {
          "ReadCapacityUnits": 2,
          "WriteCapacityUnits": 2
        }
      },
      {
        "IndexName": "ContentTypeIndex",
        "KeySchema": [
          {
            "AttributeName": "ContentType",
            "KeyType": "HASH"
          },
          {
            "AttributeName": "UploadedAt",
            "KeyType": "RANGE"
          }
        ],
        "Projection": {
          "ProjectionType": "KEYS_ONLY"
        },
        "IndexStatus": "ACTIVE",
        "ProvisionedThroughput": {
          "ReadCapacityUnits": 2,
          "WriteCapacityUnits": 2
        }
      }
    ],
    "DeletionProtectionEnabled": true
  }
}
```

**List all DynamoDB tables:**

```bash
aws dynamodb list-tables --region us-east-2
```

**Expected Output:**

```json
{
  "TableNames": ["FileUploads"]
}
```

### 9. Common Error Scenarios and Outputs

**Error: Table Already Exists**

```
Error: creating DynamoDB Table (FileUploads): ResourceInUseException: Table already exists: FileUploads
{
  RespMetadata: {
    StatusCode: 400,
    RequestID: "12345678-1234-1234-1234-123456789012"
  },
  Message_: "Table already exists: FileUploads"
}
```

**Error: Insufficient Permissions**

```
Error: creating DynamoDB Table (FileUploads): AccessDeniedException: User: arn:aws:iam::123456789012:user/terraform is not authorized to perform: dynamodb:CreateTable on resource: arn:aws:dynamodb:us-east-2:123456789012:table/FileUploads
{
  RespMetadata: {
    StatusCode: 400,
    RequestID: "12345678-1234-1234-1234-123456789012"
  },
  Message_: "User: arn:aws:iam::123456789012:user/terraform is not authorized to perform: dynamodb:CreateTable"
}
```

**Success Message After Apply:**

```
Apply complete! Resources: 15 added, 1 changed, 0 destroyed.
```

---

## 📋 What Gets Created

### DynamoDB Resources

- **FileUploads Table**: Main table for file storage
- **Global Secondary Indexes**:
  - `UploadedAtIndex`: Query by user and upload date
  - `ContentTypeIndex`: Query by content type and date
- **Point-in-Time Recovery**: Enabled for data protection
- **Server-Side Encryption**: AWS managed encryption
- **Deletion Protection**: Prevents accidental table deletion

### IAM Permissions

- **DynamoDB Policy**: Read/Write access for ECS tasks
- **Policy Attachments**: Added to existing ECS task role

### CloudWatch Monitoring

- **Read Throttle Alarms**: Monitor read capacity issues
- **Write Throttle Alarms**: Monitor write capacity issues
- **Capacity Utilization**: Alert at 80% utilization (Provisioned mode)

### ECS Integration

- **Environment Variables**: `DYNAMODB_TABLE_NAME` added to container
- **Task Role**: DynamoDB permissions attached

## ⚙️ Configuration Options

### Billing Modes

#### Provisioned (Predictable Costs)

```hcl
dynamodb_billing_mode       = "PROVISIONED"
dynamodb_read_capacity      = 5
dynamodb_write_capacity     = 5
dynamodb_gsi_read_capacity  = 2
dynamodb_gsi_write_capacity = 2
```

#### On-Demand (Pay Per Request)

```hcl
dynamodb_billing_mode = "PAY_PER_REQUEST"
# No capacity settings needed
```

### Security Settings

```hcl
# Backup and recovery
dynamodb_point_in_time_recovery = true
dynamodb_deletion_protection    = true

# Optional: TTL for automatic cleanup
dynamodb_ttl_enabled   = true
dynamodb_ttl_attribute = "ExpiresAt"  # Unix timestamp
```

### Monitoring

```hcl
enable_dynamodb_monitoring = true
sns_topic_arn = "arn:aws:sns:us-east-2:123456789012:alerts"
```

## 🔍 Troubleshooting

### Common Issues

#### 1. Table Already Exists

```
Error: ResourceInUseException: Table already exists
```

**Solution**: Either import existing table or use different name

```bash
# Import existing table
terraform import aws_dynamodb_table.file_uploads FileUploads
```

#### 2. Insufficient IAM Permissions

```
Error: AccessDeniedException: User: arn:aws:iam::...
```

**Solution**: Ensure your AWS credentials have DynamoDB permissions

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "dynamodb:CreateTable",
        "dynamodb:DescribeTable",
        "dynamodb:TagResource",
        "dynamodb:UntagResource"
      ],
      "Resource": "*"
    }
  ]
}
```

#### 3. Region Mismatch

```
Error: ResourceNotFoundException: Requested resource not found
```

**Solution**: Verify AWS region in terraform.tfvars matches your setup

### Validation Commands

```bash
# Check table status
aws dynamodb describe-table --table-name FileUploads

# List all tables
aws dynamodb list-tables

# Check IAM role permissions
aws iam get-role-policy --role-name AppRunnerInstanceRole-file-uploader-api --policy-name AppRunnerDynamoDBRW-file-uploader-api

# Test table access
aws dynamodb scan --table-name FileUploads --limit 1
```

## 💰 Cost Estimation

### Provisioned Mode (Default: 5 RCU, 5 WCU)

- **Base Table**: ~$3.17/month
- **2 GSIs (2 RCU, 2 WCU each)**: ~$2.53/month
- **Storage**: $0.25/GB/month
- **Point-in-Time Recovery**: $0.20/GB/month
- **Total**: ~$6/month + storage costs

### On-Demand Mode

- **Reads**: $0.25 per million read requests
- **Writes**: $1.25 per million write requests
- **Storage**: $0.25/GB/month
- **Good for**: Variable/unpredictable traffic

## 🔄 Migration from S3 to DynamoDB

If you want to migrate existing S3 files to DynamoDB:

### 1. Create Migration Script

```bash
# Example migration (create separate script)
aws s3 ls s3://your-bucket --recursive | while read -r line; do
  # Download from S3, upload to DynamoDB via API
done
```

### 2. Dual Write Pattern

- Keep S3 integration for large files
- Use DynamoDB for small files and metadata
- Route based on file size in your application

### 3. Blue-Green Deployment

- Deploy both S3 and DynamoDB versions
- Gradually shift traffic to DynamoDB endpoints
- Keep S3 as backup during transition

## 📚 Terraform Commands Reference

```bash
# Initialize
terraform init

# Format code
terraform fmt

# Validate configuration
terraform validate

# Plan changes
terraform plan

# Apply changes
terraform apply

# Show current state
terraform show

# List resources
terraform state list

# Show outputs
terraform output

# Destroy resources (careful!)
terraform destroy
```

## 🔧 Customization Examples

### Different Table Name

```hcl
dynamodb_table_name = "MyApp-FileStorage"
```

### Higher Capacity for Production

```hcl
dynamodb_read_capacity  = 25
dynamodb_write_capacity = 25
```

### Enable TTL for Automatic Cleanup

```hcl
dynamodb_ttl_enabled   = true
dynamodb_ttl_attribute = "ExpiresAt"

# In your app, set ExpiresAt to Unix timestamp
# File will be deleted automatically after expiration
```

### Custom Tags

```hcl
tags = {
  Project     = "MyFileUploader"
  Environment = "production"
  Team        = "DevOps"
  CostCenter  = "Engineering"
}
```

---

**🎯 Ready to Deploy!** Your infrastructure is configured and ready. Run `terraform plan` to see what will be created, then `terraform apply` to deploy your DynamoDB file upload system!

## 🐳 Docker Deployment with AWS Credentials

After deploying your DynamoDB infrastructure with Terraform, you need to run your application with proper AWS credentials to access the DynamoDB table.

### ✅ Working Docker Commands

#### **Recommended: Auto-Detect AWS Credentials with Configuration-Based Table Names**

```bash
# Stop any running containers first
docker ps -q | ForEach-Object { docker stop $_ }

# Production environment (uses FileUploads table)
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest

# Development environment (uses FileUploads-Dev table) 
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest

# Custom table name override
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e DYNAMODB__TABLENAME=FileUploads-MyCustom \
  -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) \
  -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) \
  -e AWS_DEFAULT_REGION=us-east-2 \
  fileuploaderapi:latest
```

#### **Alternative: Explicit Credentials**

```bash
# Replace with your actual AWS credentials
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development -e AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE -e AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY -e AWS_DEFAULT_REGION=us-east-2 fileuploaderapi:latest
```

#### **Alternative: Volume Mount (Windows)**

```bash
# Mount AWS credentials directory
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development -v C:\Users\%USERNAME%\.aws:/root/.aws:ro fileuploaderapi:latest
```

### 🧪 Testing the Application

1. **Build the Docker image** (if not already built):

   ```bash
   docker build -t fileuploaderapi:latest .
   ```

2. **Run with AWS credentials** using one of the commands above

3. **Access Swagger UI**: http://localhost:8080/swagger/index.html

4. **Test DynamoDB endpoints**:
   - `GET /api/DynamoFile/health` - Check DynamoDB connectivity
   - `POST /api/DynamoFile/upload` - Upload file to DynamoDB
   - `GET /api/DynamoFile` - List files in DynamoDB

### 🔍 Troubleshooting AWS Credentials

#### **Verify AWS CLI Configuration**

```bash
# Check your AWS configuration
aws configure list

# Test AWS credentials
aws dynamodb describe-table --table-name FileUploads --region us-east-2
```

#### **Common AWS Credential Errors**

```
Error: Failed to resolve AWS credentials
```

**Solutions:**

1. **Check AWS CLI setup**: Run `aws configure` to set up credentials
2. **Verify region**: Ensure `--region us-east-2` matches your table location
3. **Check IAM permissions**: Ensure your AWS user has DynamoDB permissions

#### **Verify DynamoDB Access**

```bash
# List DynamoDB tables
aws dynamodb list-tables --region us-east-2

# Check table status
aws dynamodb describe-table --table-name FileUploads --region us-east-2 --query 'Table.TableStatus'
```

### 📋 Expected Application Logs

**Successful startup with credentials:**

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:8080
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
```

**Successful DynamoDB upload:**

```
info: FileUploaderApi.Controllers.DynamoFileController[0]
      DynamoDB upload request received
info: FileUploaderApi.Controllers.DynamoFileController[0]
      File uploaded successfully to DynamoDB - Id: {FileId}, Size: {FileSize} bytes
```

### 🚀 Quick Testing Commands

```bash
# Complete workflow: Stop, Build, Run
docker ps -q | ForEach-Object { docker stop $_ }; docker build -t fileuploaderapi:latest .; docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development -e AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id) -e AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key) -e AWS_DEFAULT_REGION=us-east-2 fileuploaderapi:latest

# Test DynamoDB health endpoint
curl http://localhost:8080/api/DynamoFile/health

# Check Swagger UI
# Open: http://localhost:8080/swagger/index.html
```

---
