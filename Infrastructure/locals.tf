##############
# Local Values
##############
locals {
  # Make paths relative to this .tf file's folder
  build_context   = abspath("${path.module}/${var.docker_context}")
  dockerfile_path = "${local.build_context}/${var.dockerfile}"

  # ECR bits
  registry  = element(split("/", aws_ecr_repository.api.repository_url), 0)
  image_uri = "${aws_ecr_repository.api.repository_url}:${var.image_tag}"
}