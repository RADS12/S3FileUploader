##############
# IAM Resources for AWS Glue
##############

# Glue Service Role
resource "aws_iam_role" "glue_service_role" {
  name = "${var.tags.Project}-glue-service-role"
  
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "glue.amazonaws.com"
        }
      }
    ]
  })
  
  tags = var.tags
}

# Glue Service Policy - S3 Access
resource "aws_iam_policy" "glue_s3_policy" {
  name        = "${var.tags.Project}-glue-s3-policy"
  description = "S3 access policy for Glue jobs"
  
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject",
          "s3:ListBucket",
          "s3:GetBucketLocation"
        ]
        Resource = [
          data.aws_s3_bucket.uploads.arn,
          "${data.aws_s3_bucket.uploads.arn}/*",
          aws_s3_bucket.processed_data.arn,
          "${aws_s3_bucket.processed_data.arn}/*",
          aws_s3_bucket.glue_scripts.arn,
          "${aws_s3_bucket.glue_scripts.arn}/*"
        ]
      }
    ]
  })
  
  tags = var.tags
}

# Glue Service Policy - DynamoDB Access
resource "aws_iam_policy" "glue_dynamodb_policy" {
  name        = "${var.tags.Project}-glue-dynamodb-policy"
  description = "DynamoDB access policy for Glue jobs"
  
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "dynamodb:GetItem",
          "dynamodb:PutItem",
          "dynamodb:UpdateItem",
          "dynamodb:DeleteItem",
          "dynamodb:Query",
          "dynamodb:Scan",
          "dynamodb:DescribeTable",
          "dynamodb:BatchGetItem",
          "dynamodb:BatchWriteItem"
        ]
        Resource = [
          "arn:aws:dynamodb:${var.region}:*:table/${var.dynamodb_table_name}",
          "arn:aws:dynamodb:${var.region}:*:table/${var.dynamodb_table_name}/index/*"
        ]
      }
    ]
  })
  
  tags = var.tags
}

# CloudWatch Logs policy for Glue jobs
resource "aws_iam_policy" "glue_cloudwatch_policy" {
  name        = "${var.tags.Project}-glue-cloudwatch-policy"
  description = "CloudWatch logs access for Glue jobs"
  
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents",
          "logs:DescribeLogGroups",
          "logs:DescribeLogStreams"
        ]
        Resource = [
          "arn:aws:logs:${var.region}:*:log-group:/aws-glue/*",
          "arn:aws:logs:${var.region}:*:log-group:/aws-glue/*:log-stream:*"
        ]
      }
    ]
  })
  
  tags = var.tags
}

# Glue Catalog policy
resource "aws_iam_policy" "glue_catalog_policy" {
  name        = "${var.tags.Project}-glue-catalog-policy"
  description = "Glue Data Catalog access policy"
  
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "glue:GetDatabase",
          "glue:GetDatabases",
          "glue:CreateTable",
          "glue:GetTable",
          "glue:GetTables",
          "glue:UpdateTable",
          "glue:DeleteTable",
          "glue:GetPartition",
          "glue:GetPartitions",
          "glue:CreatePartition",
          "glue:UpdatePartition",
          "glue:DeletePartition",
          "glue:BatchCreatePartition",
          "glue:BatchDeletePartition"
        ]
        Resource = [
          "arn:aws:glue:${var.region}:*:catalog",
          "arn:aws:glue:${var.region}:*:database/${var.tags.Project}-file-processing",
          "arn:aws:glue:${var.region}:*:table/${var.tags.Project}-file-processing/*"
        ]
      }
    ]
  })
  
  tags = var.tags
}

# Attach AWS managed Glue service policy
resource "aws_iam_role_policy_attachment" "glue_service_role_policy" {
  role       = aws_iam_role.glue_service_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSGlueServiceRole"
}

# Attach custom S3 policy
resource "aws_iam_role_policy_attachment" "glue_s3_policy_attachment" {
  role       = aws_iam_role.glue_service_role.name
  policy_arn = aws_iam_policy.glue_s3_policy.arn
}

# Attach custom DynamoDB policy
resource "aws_iam_role_policy_attachment" "glue_dynamodb_policy_attachment" {
  role       = aws_iam_role.glue_service_role.name
  policy_arn = aws_iam_policy.glue_dynamodb_policy.arn
}

# Attach CloudWatch policy
resource "aws_iam_role_policy_attachment" "glue_cloudwatch_policy_attachment" {
  role       = aws_iam_role.glue_service_role.name
  policy_arn = aws_iam_policy.glue_cloudwatch_policy.arn
}

# Attach Glue Catalog policy
resource "aws_iam_role_policy_attachment" "glue_catalog_policy_attachment" {
  role       = aws_iam_role.glue_service_role.name
  policy_arn = aws_iam_policy.glue_catalog_policy.arn
}