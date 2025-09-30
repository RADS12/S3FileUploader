##############
# Outputs
##############
output "service_url" { 
  value       = "http://${aws_lb.api.dns_name}"
  description = "The Load Balancer URL for the ECS service"
}

output "ecr_repository" { 
  value       = aws_ecr_repository.api.repository_url 
  description = "ECR repository URL"
}

output "image_tag_used" { 
  value       = var.image_tag 
  description = "Docker image tag used for deployment"
}

output "bucket_in_use" { 
  value       = data.aws_s3_bucket.uploads.bucket 
  description = "S3 bucket name used for file uploads"
}

output "ecs_cluster" {
  value       = aws_ecs_cluster.api.name
  description = "ECS cluster name"
}

output "load_balancer_dns" {
  value       = aws_lb.api.dns_name
  description = "Application Load Balancer DNS name"
}

##############
# DynamoDB Outputs
##############
output "dynamodb_table_name" {
  value       = aws_dynamodb_table.file_uploads.name
  description = "DynamoDB table name for file uploads"
}

output "dynamodb_table_arn" {
  value       = aws_dynamodb_table.file_uploads.arn
  description = "DynamoDB table ARN"
}

output "dynamodb_gsi_names" {
  value       = [for gsi in aws_dynamodb_table.file_uploads.global_secondary_index : gsi.name]
  description = "List of Global Secondary Index names"
}

output "dynamodb_billing_mode" {
  value       = aws_dynamodb_table.file_uploads.billing_mode
  description = "DynamoDB billing mode"
}

output "dynamodb_region" {
  value       = var.region
  description = "AWS region where DynamoDB table is deployed"
}

##############
# Glue Outputs
##############
output "glue_database_name" {
  description = "Name of the Glue catalog database"
  value       = var.enable_glue_integration ? aws_glue_catalog_database.file_processing_db.name : null
}

output "glue_crawler_name" {
  description = "Name of the Glue crawler"
  value       = var.enable_glue_integration ? aws_glue_crawler.raw_files_crawler[0].name : null
}

output "glue_processed_data_bucket" {
  description = "S3 bucket for processed data"
  value       = aws_s3_bucket.processed_data.bucket
}

output "glue_scripts_bucket" {
  description = "S3 bucket for Glue scripts"
  value       = aws_s3_bucket.glue_scripts.bucket
}

output "glue_service_role_arn" {
  description = "ARN of the Glue service role"
  value       = aws_iam_role.glue_service_role.arn
}

output "glue_csv_job_name" {
  description = "Name of the CSV processing Glue job"
  value       = var.enable_glue_csv_processing ? aws_glue_job.csv_processor[0].name : null
}

output "glue_json_job_name" {
  description = "Name of the JSON processing Glue job"
  value       = var.enable_glue_json_processing ? aws_glue_job.json_processor[0].name : null
}