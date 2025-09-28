# Web API Creation Commands Reference

This document contains all the commands used to create, build, deploy, and troubleshoot the S3 File Uploader Web API project.

## Table of Contents

- [Initial Project Setup](#initial-project-setup)
- [Package Management](#package-management)
- [Local Development](#local-development)
- [Docker Commands](#docker-commands)
- [Git Commands](#git-commands)
- [AWS ECR Setup](#aws-ecr-setup)
- [Terraform Deployment](#terraform-deployment)
- [Troubleshooting Commands](#troubleshooting-commands)
- [Final Working Solution](#final-working-solution)

---

## Initial Project Setup

### Create Solution and Projects

```bash
# Create a folder for your solution
mkdir WebApiDemo
cd WebApiDemo

# Create solution
dotnet new sln -n WebApiDemo

# Create Web API project
dotnet new webapi -n MyWebApi

# Add Web API project to solution
dotnet sln add MyWebApi/MyWebApi.csproj

# (Optional) Create unit test project
dotnet new xunit -n MyWebApi.Tests
dotnet sln add MyWebApi.Tests/MyWebApi.Tests.csproj
dotnet add MyWebApi.Tests/MyWebApi.Tests.csproj reference MyWebApi/MyWebApi.csproj

# Build solution
dotnet build

# Run Web API (local dev)
cd MyWebApi
dotnet run

# Run tests (if added)
cd ../MyWebApi.Tests
dotnet test
```

### Publishing Commands

```bash
# Go back to WebApi project folder
cd ../MyWebApi

# Publish for local folder output
dotnet publish -c Release -o ./publish

# Publish self-contained (include runtime, replace win-x64 with linux-x64 if needed)
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

---

## Package Management

### Add Required Packages

```bash
# Add AWS S3 SDK
dotnet add package AWSSDK.S3

# Add Swagger/OpenAPI support
dotnet add FileUploaderApi package Swashbuckle.AspNetCore
```

---

## Local Development

### Build and Run Commands

```bash
# In project folder:
dotnet clean
dotnet build
dotnet run
```

### Access Local API

```bash
# Local development URL
http://localhost:8080/swagger
```

### Manual API Testing

```bash
# Upload file (multipart to API)
curl -F "file=@./somefile.pdf" http://localhost:8080/api/fileupload/upload

# Get presigned download URL
curl "http://localhost:8080/api/fileupload/download-url/<key>"

# Get presigned upload URL (client PUT directly to S3)
curl -X POST http://localhost:8080/api/fileupload/upload-url \
  -H "Content-Type: application/json" \
  -d '{"desiredKey":"myfolder/test.bin","minutes":15}'

# Then PUT the file directly
curl -X PUT "<returned url>" --data-binary @./test.bin -H "Content-Type: application/octet-stream"
```

---

## Docker Commands

### Basic Docker Operations

```bash
# Build Docker image
docker build -t fileuploaderapi .

# Run container locally
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development fileuploaderapi

# OR from the solution root (where Dockerfile is)
docker build -t fileuploaderapi:latest .
docker run -p 8080:8080 fileuploaderapi:latest
```

### Optimized Docker Commands (After .dockerignore fix)

```bash
# Build with dev tag (much faster after .dockerignore optimization)
docker build -t file-uploader-api:dev .

# Clean up Docker system
docker system prune -f
```

---

## Git Commands

### Initial Git Setup

```bash
# Go to your solution root folder (where .sln is)
cd "C:\Path\To\S3FileUploader"

# Initialize a new git repo (only once)
git init

# Add all files for commit
git add .

# Commit the files
git commit -m "Initial commit for S3FileUploader"

# Rename current branch to main (GitHub default)
git branch -M main

# Add remote (link local repo to GitHub repo)
git remote add origin https://github.com/Rads12/S3FileUploader.git

# Verify the remote is correct
git remote -v

# Push local main branch to remote (first time uses -u)
git push -u origin main
```

### Git Troubleshooting Commands

```bash
# If remote was already set and wrong, update instead:
git remote set-url origin https://github.com/Rads12/S3FileUploader.git

# If remote already had commits (like README), sync before pushing
git pull --rebase origin main
git push -u origin main

# Check which branches are tracking which remotes
git branch -vv

# Set local main to track remote main explicitly
git branch -u origin/main

# If you ever need to force overwrite remote with your local (use with caution!)
git push --force-with-lease origin main
```

---

## AWS ECR Setup

### ECR Repository Creation and Docker Push

```bash
# Set region
AWS_REGION=us-east-2

# Get account ID
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)

# Create ECR repo (safe to re-run; it errors if exists)
aws ecr create-repository --repository-name fileuploaderapi --region $AWS_REGION || true

# Login Docker to ECR
aws ecr get-login-password --region $AWS_REGION | \
  docker login --username AWS --password-stdin ${ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com

# Tag & push
docker tag fileuploaderapi:latest ${ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com/fileuploaderapi:latest
docker push ${ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com/fileuploaderapi:latest
```

### ECR Manual Push (Working Commands)

```bash
# Tag the image
docker tag file-uploader-api:dev 675016865089.dkr.ecr.us-east-2.amazonaws.com/file-uploader-api:dev

# Login to ECR
aws ecr get-login-password --region us-east-2 | docker login --username AWS --password-stdin 675016865089.dkr.ecr.us-east-2.amazonaws.com

# Push to ECR
docker push 675016865089.dkr.ecr.us-east-2.amazonaws.com/file-uploader-api:dev
```

---

## Terraform Deployment

### Basic Terraform Commands

```bash
# Format and validate
terraform fmt      # optional: auto-format
terraform validate

# Initialize and show current state
terraform init
terraform show     # (optional)
```

### Terraform with Variables

```bash
# Use timestamp tag
$tag = (Get-Date -Format "yyyyMMdd-HHmmss")

# Or use git short SHA
$tag = (git rev-parse --short HEAD)

# Or use both (recommended)
$tag = "$(git rev-parse --short HEAD)-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# Apply with variables
terraform apply -auto-approve -var="bucket_name=rad-s3-demo-first-1" -var="image_tag=$tag" -var="docker_context=../s3fileuploader" -var="dockerfile=Dockerfile"
```

### Environment Variable Setup

```bash
# Set variables via environment (PowerShell)
$env:TF_VAR_bucket_name = "rad-s3-demo-first-1"
$env:TF_VAR_image_tag = "dev"
terraform apply

# Or via CLI
terraform apply -var="bucket_name=rad-s3-demo-first-1" -var="image_tag=dev"
```

### terraform.tfvars File Example

Create a `terraform.tfvars` file with:

```hcl
bucket_name = "rad-s3-demo-first-1"
image_tag   = "dev"
docker_context = "../s3fileuploader"  # path to folder containing Dockerfile
dockerfile = "Dockerfile"             # Dockerfile name (if not standard)
region = "us-east-2"                  # AWS region
repo_name = "file-uploader-api"       # ECR repository name
```

Then run:

```bash
terraform apply
```

---

## Troubleshooting Commands

### Docker and AWS Verification

```bash
# Verify Docker works
docker version
docker info
docker run --rm hello-world

# Verify AWS CLI + account/region
aws sts get-caller-identity
aws ecr describe-repositories --region us-east-2

# Manually test ECR login
aws ecr get-login-password --region us-east-2 | docker login --username AWS --password-stdin 675016865089.dkr.ecr.us-east-2.amazonaws.com

# Check ECR repository
aws ecr describe-repositories --repository-names file-uploader-api --region us-east-2
```

### ECS Service Monitoring

```bash
# Check ECS service status
aws ecs describe-services --cluster file-uploader-api --services file-uploader-api --query "services[0].{RunningCount:runningCount,DesiredCount:desiredCount,Status:status}" --output table

# List ECS tasks
aws ecs list-tasks --cluster file-uploader-api --service-name file-uploader-api

# Check specific task status
aws ecs describe-tasks --cluster file-uploader-api --tasks [TASK-ARN] --query "tasks[0].{LastStatus:lastStatus,DesiredStatus:desiredStatus,HealthStatus:healthStatus}" --output table

# Force new deployment
aws ecs update-service --cluster file-uploader-api --service file-uploader-api --force-new-deployment --region us-east-2

# Scale service down for maintenance
aws ecs update-service --cluster file-uploader-api --service file-uploader-api --desired-count 0 --region us-east-2
```

### Load Balancer Health Checks

```bash
# Check target group health
aws elbv2 describe-target-health --target-group-arn [TARGET-GROUP-ARN]

# Check load balancer listeners
aws elbv2 describe-listeners --load-balancer-arn [LOAD-BALANCER-ARN] --region us-east-2

# Check load balancer details
aws elbv2 describe-load-balancers --names file-uploader-api --query "LoadBalancers[0].{Scheme:Scheme,State:State,AvailabilityZones:AvailabilityZones}" --output json
```

### Container Logs and Diagnostics

```bash
# Get container logs (CloudWatch)
aws logs get-log-events --log-group-name "/ecs/file-uploader-api" --log-stream-name "ecs/file-uploader-api/[TASK-ID]" --start-time 1000000000000 --query "events[*].message" --output text

# Tail logs (real-time)
aws logs tail /ecs/file-uploader-api --region us-east-2 --since 10m

# Check security groups
aws ec2 describe-security-groups --group-ids [SECURITY-GROUP-ID] --query "SecurityGroups[0].{GroupId:GroupId,InboundRules:IpPermissions}" --output table
```

### Network Connectivity Tests

```bash
# DNS lookup
nslookup file-uploader-api-1896670076.us-east-2.elb.amazonaws.com

# Test port connectivity (PowerShell)
Test-NetConnection -ComputerName file-uploader-api-1896670076.us-east-2.elb.amazonaws.com -Port 80

# HTTP requests (PowerShell)
Invoke-RestMethod -Uri "http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/swagger/index.html" -TimeoutSec 10
Invoke-RestMethod -Uri "http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/swagger/v1/swagger.json" -TimeoutSec 10
```

---

## Load Balancer Target Group Management

### Target Group Dependency Resolution

When changing ports or target groups, manual intervention may be required:

```bash
# Create new target group for port 8080
aws elbv2 create-target-group --name file-uploader-api-port8080 --protocol HTTP --port 8080 --vpc-id vpc-04e1ba06138b7ffe7 --target-type ip --health-check-path /health --region us-east-2

# Update listener to use new target group
aws elbv2 modify-listener --listener-arn [LISTENER-ARN] --default-actions Type=forward,TargetGroupArn=[NEW-TARGET-GROUP-ARN] --region us-east-2

# Delete old target group
aws elbv2 delete-target-group --target-group-arn [OLD-TARGET-GROUP-ARN] --region us-east-2

# Import new target group into Terraform state
terraform state rm aws_lb_target_group.api
terraform import aws_lb_target_group.api [NEW-TARGET-GROUP-ARN]
```

---

## Final Working Solution

### Successful Deployment Commands

```bash
# Final working directory navigation
cd "C:\Users\radkr\OneDrive\Documents\Projects\S3FileUploader"

# Build optimized Docker image
docker build -t file-uploader-api:dev .

# Navigate to Infrastructure
cd "C:\Users\radkr\OneDrive\Documents\Projects\S3FileUploader\Infrastructure"

# Apply final Terraform configuration
terraform apply -auto-approve
```

### Final Outputs

```
bucket_in_use = "rad-s3-demo-first-1"
ecr_repository = "675016865089.dkr.ecr.us-east-2.amazonaws.com/file-uploader-api"
ecs_cluster = "file-uploader-api"
image_tag_used = "dev"
load_balancer_dns = "file-uploader-api-1896670076.us-east-2.elb.amazonaws.com"
service_url = "http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com"
```

### Final Working URLs

```bash
# Swagger UI
http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/swagger/index.html

# API Base URL
http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com

# Test file upload
curl -F "file=@C:\Path\To\Your\File.pdf" http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/api/fileupload/upload

# Test download URL generation
curl "http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/api/fileupload/download-url/<key>"
```

---

## Key Lessons Learned

### Issues Encountered and Solutions

1. **App Runner Subscription Error**: Switched to ECS Fargate as alternative
2. **Port Permission Denied**: Changed from port 80 to 8080 for non-root containers
3. **Target Group Dependencies**: Manual AWS CLI operations to resolve conflicts
4. **Security Group Configuration**: Separated ALB and ECS security groups
5. **Docker Build Context**: Optimized with .dockerignore (532MB → 846B)

### Production Recommendations

- Add HTTPS with SSL certificate
- Implement proper authentication
- Set up auto-scaling policies
- Add comprehensive monitoring
- Follow security best practices
- Implement CI/CD pipeline

---

## Notes

- Ensure AWS credentials have necessary permissions for ECR, ECS, S3, IAM, VPC, etc.
- Clean up resources with `terraform destroy` when done to avoid charges
- Adjust region, instance sizes, and scaling as needed for your use case
- Monitor logs via CloudWatch for troubleshooting
- For production, consider using HTTPS with a proper domain and certificate
- This setup is basic and can be enhanced with CI/CD, autoscaling, backups, etc.
- Always follow best practices for security, cost management, and scalability

---

_Last Updated: September 27, 2025_
