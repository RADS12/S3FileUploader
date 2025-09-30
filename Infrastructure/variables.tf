##############
# Variables
##############
variable "region" {
  type    = string
  default = "us-east-2"
}

variable "bucket_name" {
  type        = string
  description = "S3 bucket name for file uploads"
  default     = "rad-s3-demo-first-1"
}

variable "repo_name" {
  type    = string
  default = "file-uploader-api"
}

variable "image_tag" {
  type    = string
  default = "dev" # e.g., 20250925-1412 or git-sha
}

variable "docker_context" {
  type    = string
  default = ".." # path to folder containing Dockerfile (project root)
}

variable "dockerfile" {
  type    = string
  default = "Dockerfile"
}

variable "docker_platform" {
  type    = string
  default = "linux/amd64"
}

variable "tags" {
  type = map(string)
  default = {
    Project = "file-uploader-api"
    Owner   = "rad"
    Env     = "dev"
  }
}

##############
# DynamoDB Variables
##############
variable "dynamodb_table_name" {
  type        = string
  description = "DynamoDB table name for file uploads"
  default     = "FileUploads"
}

variable "dynamodb_billing_mode" {
  type        = string
  description = "DynamoDB billing mode (PROVISIONED or PAY_PER_REQUEST)"
  default     = "PROVISIONED"
  
  validation {
    condition     = contains(["PROVISIONED", "PAY_PER_REQUEST"], var.dynamodb_billing_mode)
    error_message = "Billing mode must be either PROVISIONED or PAY_PER_REQUEST."
  }
}

variable "dynamodb_read_capacity" {
  type        = number
  description = "DynamoDB read capacity units (only used with PROVISIONED billing)"
  default     = 5
}

variable "dynamodb_write_capacity" {
  type        = number
  description = "DynamoDB write capacity units (only used with PROVISIONED billing)"
  default     = 5
}

variable "dynamodb_gsi_read_capacity" {
  type        = number
  description = "DynamoDB GSI read capacity units (only used with PROVISIONED billing)"
  default     = 2
}

variable "dynamodb_gsi_write_capacity" {
  type        = number
  description = "DynamoDB GSI write capacity units (only used with PROVISIONED billing)"
  default     = 2
}

variable "dynamodb_point_in_time_recovery" {
  type        = bool
  description = "Enable point-in-time recovery for DynamoDB table"
  default     = true
}

variable "dynamodb_kms_key_id" {
  type        = string
  description = "KMS key ID for DynamoDB encryption (leave empty for AWS managed key)"
  default     = ""
}

variable "dynamodb_deletion_protection" {
  type        = bool
  description = "Enable deletion protection for DynamoDB table"
  default     = true
}

variable "dynamodb_ttl_enabled" {
  type        = bool
  description = "Enable TTL for automatic cleanup of old files"
  default     = false
}

variable "dynamodb_ttl_attribute" {
  type        = string
  description = "Attribute name for TTL (must be a Unix timestamp)"
  default     = "ExpiresAt"
}

variable "enable_dynamodb_resource_policy" {
  type        = bool
  description = "Enable DynamoDB resource policy for fine-grained access control"
  default     = false
}

variable "enable_dynamodb_monitoring" {
  type        = bool
  description = "Enable CloudWatch alarms for DynamoDB monitoring"
  default     = true
}

variable "sns_topic_arn" {
  type        = string
  description = "SNS topic ARN for CloudWatch alarm notifications"
  default     = ""
}

##############
# Glue Variables
##############
variable "enable_glue_integration" {
  type        = bool
  description = "Enable AWS Glue integration for file processing"
  default     = true
}

variable "glue_version" {
  type        = string
  description = "AWS Glue version to use"
  default     = "4.0"
}

variable "glue_crawler_schedule" {
  type        = string
  description = "Cron expression for Glue crawler schedule"
  default     = "cron(0 12 * * ? *)"  # Daily at noon
}

variable "enable_glue_csv_processing" {
  type        = bool
  description = "Enable CSV processing Glue job"
  default     = true
}

variable "enable_glue_json_processing" {
  type        = bool
  description = "Enable JSON processing Glue job"
  default     = false
}

variable "glue_max_concurrent_runs" {
  type        = number
  description = "Maximum concurrent runs for Glue jobs"
  default     = 3
}

variable "glue_worker_type" {
  type        = string
  description = "Glue worker type (G.1X, G.2X, G.025X)"
  default     = "G.1X"
  
  validation {
    condition     = contains(["G.1X", "G.2X", "G.025X"], var.glue_worker_type)
    error_message = "Worker type must be G.1X, G.2X, or G.025X."
  }
}

variable "glue_number_of_workers" {
  type        = number
  description = "Number of Glue workers"
  default     = 2
}