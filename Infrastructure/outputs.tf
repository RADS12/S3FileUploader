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