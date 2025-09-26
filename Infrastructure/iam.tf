##############
# IAM for App Runner
# 1) Access role (pull image from ECR)
# 2) Instance role (runtime credentials for S3)
##############

# 1) Access role for ECR pulling
data "aws_iam_policy_document" "apprunner_access_trust" {
  statement {
    effect = "Allow"
    principals {
      type        = "Service"
      identifiers = ["build.apprunner.amazonaws.com"]
    }
    actions = ["sts:AssumeRole"]
  }
}

resource "aws_iam_role" "apprunner_access_role" {
  name               = "AppRunnerAccessRole-${var.repo_name}"
  assume_role_policy = data.aws_iam_policy_document.apprunner_access_trust.json
  tags               = var.tags
}

# ECR pull permissions policy
data "aws_iam_policy_document" "apprunner_ecr_pull" {
  statement {
    effect = "Allow"
    actions = [
      "ecr:GetAuthorizationToken",
      "ecr:BatchCheckLayerAvailability",
      "ecr:GetDownloadUrlForLayer",
      "ecr:BatchGetImage"
    ]
    resources = ["*"]
  }
}

resource "aws_iam_policy" "apprunner_ecr_pull" {
  name   = "AppRunnerECRPull-${var.repo_name}"
  policy = data.aws_iam_policy_document.apprunner_ecr_pull.json
}

resource "aws_iam_role_policy_attachment" "apprunner_access_attach" {
  role       = aws_iam_role.apprunner_access_role.name
  policy_arn = aws_iam_policy.apprunner_ecr_pull.arn
}

# 2) Instance role for runtime permissions
data "aws_iam_policy_document" "apprunner_instance_trust" {
  statement {
    effect = "Allow"
    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com", "tasks.apprunner.amazonaws.com"]
    }
    actions = ["sts:AssumeRole"]
  }
}

resource "aws_iam_role" "apprunner_instance_role" {
  name               = "AppRunnerInstanceRole-${var.repo_name}"
  assume_role_policy = data.aws_iam_policy_document.apprunner_instance_trust.json
  tags               = var.tags
}

# S3 read/write permissions for the app
data "aws_iam_policy_document" "s3_rw" {
  statement {
    sid       = "ListBucket"
    effect    = "Allow"
    actions   = ["s3:ListBucket"]
    resources = [data.aws_s3_bucket.uploads.arn]
  }
  statement {
    sid       = "ObjectRW"
    effect    = "Allow"
    actions   = ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"]
    resources = ["${data.aws_s3_bucket.uploads.arn}/*"]
  }
}

resource "aws_iam_policy" "s3_rw" {
  name   = "AppRunnerS3RW-${var.repo_name}"
  policy = data.aws_iam_policy_document.s3_rw.json
}

resource "aws_iam_role_policy_attachment" "apprunner_instance_attach" {
  role       = aws_iam_role.apprunner_instance_role.name
  policy_arn = aws_iam_policy.s3_rw.arn
}

# ECS Execution Role (for pulling images and logs)
data "aws_iam_policy_document" "ecs_execution_trust" {
  statement {
    effect = "Allow"
    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
    actions = ["sts:AssumeRole"]
  }
}

resource "aws_iam_role" "ecs_execution_role" {
  name               = "ECSExecutionRole-${var.repo_name}"
  assume_role_policy = data.aws_iam_policy_document.ecs_execution_trust.json
  tags               = var.tags
}

# Attach AWS managed policy for ECS task execution
resource "aws_iam_role_policy_attachment" "ecs_execution_attach" {
  role       = aws_iam_role.ecs_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}