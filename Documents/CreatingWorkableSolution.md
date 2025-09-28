# Creating a Workable .NET S3 File Uploader Solution - Step by Step Guide

**Date**: September 26, 2025  
**Project**: S3FileUploader - .NET Web API with AWS ECS Fargate Deployment

## 🌐 Live API Access

- **Swagger UI**: [http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/swagger/index.html](http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/swagger/index.html)
- **API Base URL**: `http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com`

### Available API Endpoints

- `POST /api/FileUpload/upload` - Upload files to S3
- `GET /api/FileUpload/download-url/{key}` - Get presigned download URLs

---

## 1. Initial Setup - Creating .gitignore for .NET Project

**Command**: Created comprehensive .gitignore file  
**Why**: Exclude build artifacts, IDE files, and sensitive data from version control  
**Result**: Successfully created .gitignore with comprehensive rules for .NET projects

- ✅ Excluded build folders (`**/bin/`, `**/obj/`)
- ✅ Excluded Visual Studio files (`.vs/`, `*.user`)
- ✅ Excluded environment files (`.env`)
- ✅ Added AWS and Terraform exclusions

---

## 2. Terraform Organization - Modularizing Infrastructure

**Command**: Refactored monolithic main.tf into modular files  
**Why**: Improve maintainability, readability, and organization of infrastructure code  
**Result**: Successfully created 9 modular Terraform files:

- 📁 `providers.tf` - AWS provider configuration
- 📁 `variables.tf` - Input variables and configuration
- 📁 `s3.tf` - S3 bucket data source
- 📁 `ecr.tf` - ECR repository for Docker images
- 📁 `iam.tf` - IAM roles and policies
- 📁 `locals.tf` - Local values and computed image URI
- 📁 `docker.tf` - Docker build and push automation
- 📁 `ecs-fargate.tf` - ECS Fargate service with load balancer
- 📁 `outputs.tf` - Output values for reference

---

## 3. Docker Optimization - Reducing Build Context

**Command**: Created optimized .dockerignore file  
**Why**: Docker build context was 532MB, causing slow builds and large image transfers  
**Result**: Reduced build context from **532MB to 846B** 📉

- ✅ Excluded unnecessary files (`bin/`, `obj/`, `.git/`, etc.)
- ✅ Improved build performance significantly
- ✅ Reduced network transfer time to ECR

### ⚠️ Issue Resolved

**Error**: Initial Docker builds were extremely slow  
**Solution**: Added comprehensive .dockerignore with proper exclusions  
**Command**: Created .dockerignore with build artifacts and IDE file exclusions

---

## 4. AWS App Runner Deployment Attempt

**Command**: `terraform apply` (initial attempt with App Runner)  
**Why**: Deploy containerized .NET API using AWS App Runner

### ❌ Error Encountered

**Error**: "Subscription limit exceeded" - App Runner not available in account  
**Solution**: Switch to ECS Fargate as alternative container hosting service  
**Explanation**: App Runner has subscription limits, ECS Fargate provides similar serverless container hosting

---

## 5. ECS Fargate Migration - Container Service Switch

**Command**: Created `ecs-fargate.tf` with ECS service definition  
**Why**: Replace App Runner with ECS Fargate for serverless container hosting  
**Result**: Successfully configured ECS Fargate infrastructure:

- 🐳 ECS Cluster for container orchestration
- 📋 ECS Task Definition with container specifications
- ⚖️ Application Load Balancer for traffic routing
- 🔒 Security groups for network access control

---

## 6. Docker Build and ECR Push

**Command**: `terraform apply` (with Docker build automation)  
**Why**: Build Docker image and push to ECR repository  
**Result**: Successfully built and pushed Docker image

- 🏷️ Image tagged as `"dev"`
- 📦 Pushed to ECR: `675016865089.dkr.ecr.us-east-2.amazonaws.com/file-uploader-api:dev`
- 🤖 Automated via Terraform `null_resource` with `local-exec`

---

## 7. Port Permission Error Resolution

**Command**: `aws logs get-log-events` (container logs inspection)  
**Why**: Containers were failing to start, needed to diagnose the issue

### ⚠️ Critical Issue Resolved

**Error**: "Permission denied" when binding to port 80  
**Root Cause**: Non-root user in container cannot bind to privileged ports (<1024)  
**Solution**: Changed container port from **80 → 8080**

#### Commands Used:

```bash
# Updated Dockerfile to expose port 8080
# Modified ECS task definition container port to 8080
# Updated ASPNETCORE_URLS environment variable to http://+:8080
```

**Explanation**: Ports below 1024 require root privileges, but containers run as non-root for security

---

## 8. IAM Role Trust Policy Fixes

**Command**: `terraform apply` (IAM role updates)  
**Why**: ECS tasks require different trust policies than App Runner

### ⚠️ Issue Resolved

**Error**: ECS tasks couldn't assume IAM roles due to incorrect trust policies  
**Solution**: Updated IAM trust policies to support both App Runner and ECS  
**Commands Used**: Modified trust policy in `iam.tf` to include both service principals:

- `ecs-tasks.amazonaws.com` (for ECS)
- `apprunner.amazonaws.com` (for App Runner compatibility)

---

## 9. Target Group Dependency Conflicts

**Command**: `terraform apply` (ECS service with load balancer)  
**Why**: Deploy ECS service with proper load balancer integration

### ⚠️ Complex Issue Resolved

**Error**: Target group conflicts between old (port 80) and new (port 8080) configurations  
**Solution**: Manual AWS CLI operations to resolve dependencies

#### Commands Used:

```bash
# 1. Remove old port 80 target group
aws elbv2 delete-target-group --target-group-arn [old-arn]

# 2. Create new target group for port 8080
aws elbv2 create-target-group --name file-uploader-api-port8080 --port 8080

# 3. Update listener to forward to new target group
aws elbv2 modify-listener --listener-arn [listener-arn] \
  --default-actions Type=forward,TargetGroupArn=[new-arn]
```

---

## 10. Terraform State Management

**Command**: `terraform state rm aws_lb_target_group.api`  
**Why**: Remove old target group from Terraform state to allow import of manually created one  
**Result**: Successfully removed resource from state

**Command**: `terraform import aws_lb_target_group.api [target-group-arn]`  
**Why**: Import manually created target group into Terraform state for management  
**Result**: Successfully imported new target group with port 8080 configuration

---

## 11. Security Group Configuration Error

**Command**: `Test-NetConnection -ComputerName [load-balancer-dns] -Port 80`  
**Why**: Test external connectivity to load balancer

### ❌ Major Networking Issue

**Error**: Connection timeout - port 80 not accessible from internet  
**Root Cause**: Load balancer using ECS security group that only allowed port 8080  
**Solution**: Created separate security groups for ALB and ECS

#### Commands Used:

1. Created `aws_security_group.alb` - allows inbound port 80 from internet
2. Modified `aws_security_group.ecs` - allows inbound port 8080 from ALB only
3. Updated `aws_lb.api` to use ALB security group

**Explanation**: Proper security segmentation - ALB accepts public traffic on port 80, forwards to ECS on port 8080

---

## 12. Final Deployment Success

**Command**: `terraform apply` (final configuration)  
**Why**: Apply all security group and networking fixes  
**Result**: ✅ Successful deployment with full functionality

### Verification Commands:

```bash
# 1. Test network connectivity
Test-NetConnection -ComputerName [load-balancer-dns] -Port 80
# RESULT: Connection successful ✅

# 2. Test Swagger UI
Invoke-RestMethod -Uri "http://[load-balancer-dns]/swagger/index.html"
# RESULT: Swagger UI loaded successfully ✅

# 3. Check ECS service status
aws ecs describe-services --cluster file-uploader-api --services file-uploader-api
# RESULT: RunningCount: 1, DesiredCount: 1, Status: ACTIVE ✅

# 4. Verify target health
aws elbv2 describe-target-health --target-group-arn [target-group-arn]
# RESULT: Target healthy ✅
```

---

## 🏗️ Final Architecture Summary

### Successful Deployment Components:

- ✅ .NET 9.0 Web API containerized with Docker
- ✅ AWS ECR for container image storage
- ✅ AWS ECS Fargate for serverless container hosting
- ✅ Application Load Balancer for HTTP traffic routing (port 80 → 8080)
- ✅ Proper security groups (ALB: public port 80, ECS: internal port 8080)
- ✅ IAM roles for ECS execution and S3 access
- ✅ CloudWatch logging for monitoring
- ✅ S3 integration for file upload functionality

### Access Endpoints:

- **API Base URL**: `http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com`
- **Swagger UI**: `http://file-uploader-api-1896670076.us-east-2.elb.amazonaws.com/swagger/index.html`
- **API Endpoints**:
  - `POST /api/FileUpload/upload` (multipart file upload)
  - `GET /api/FileUpload/download-url/{key}` (generate S3 presigned URL)

---

## 🎓 Key Lessons Learned

1. **Port Security**: Non-root containers cannot bind to privileged ports (<1024)
2. **Network Segmentation**: Load balancer and ECS tasks need separate security groups for proper traffic flow
3. **Dependency Management**: Target group dependencies require careful management during infrastructure changes
4. **Build Optimization**: Docker build context optimization significantly improves deployment speed
5. **State Management**: Terraform state management is crucial when manually creating resources
6. **Alternative Solutions**: ECS Fargate provides excellent alternative to App Runner for container hosting

---

## 💰 Cost Optimization Notes

### Current Configuration:

- **ECS Fargate**: 0.5 vCPU, 1GB memory (minimal cost for dev/test)
- **Application Load Balancer**: Pay per hour + data processed
- **ECR**: Storage costs for Docker images
- **S3**: Storage + request costs for uploaded files

### Production Recommendations:

- 📈 Scale ECS service based on traffic (`desired_count` adjustment)
- 🔄 Implement auto-scaling policies for variable load
- 🔄 Consider using ALB target group health checks for zero-downtime deployments
- 🔐 Add HTTPS listener with SSL certificate for production use
- 📊 Implement CloudWatch alarms for monitoring and alerting

---

**✨ Project Status**: Successfully Deployed and Fully Functional! 🚀
