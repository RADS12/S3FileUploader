##############
# ECR Repository
##############
resource "aws_ecr_repository" "api" {
  name = var.repo_name
  image_scanning_configuration { 
    scan_on_push = true 
  }
  force_delete = true
  tags         = var.tags
}