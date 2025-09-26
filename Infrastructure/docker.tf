##############
# Docker Build and Push - TEMPORARILY DISABLED
##############

# Prechecks: Docker running + paths exist (Windows PowerShell)
resource "null_resource" "precheck" {
  provisioner "local-exec" {
    interpreter = ["PowerShell", "-Command"]
    command     = <<-EOT
      $ErrorActionPreference = "Stop"
      Write-Host "Docker precheck temporarily disabled - manual build required"
    EOT
  }
}

# Build & push image to ECR (PowerShell) - TEMPORARILY DISABLED
resource "null_resource" "build_and_push" {
  triggers = {
    image_tag = var.image_tag # change tag to force rebuild
  }

  provisioner "local-exec" {
    interpreter = ["PowerShell", "-Command"]
    command     = <<-EOT
      $ErrorActionPreference = "Stop"
      Write-Host "Docker build temporarily disabled"
      Write-Host "Manual build required:"
      Write-Host "1. docker build -t file-uploader-api:dev ."
      Write-Host "2. docker tag file-uploader-api:dev ${local.image_uri}"
      Write-Host "3. aws ecr get-login-password --region ${var.region} | docker login --username AWS --password-stdin ${local.registry}"
      Write-Host "4. docker push ${local.image_uri}"
    EOT
  }

  depends_on = [
    aws_ecr_repository.api,
    null_resource.precheck
  ]
}