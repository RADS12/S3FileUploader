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