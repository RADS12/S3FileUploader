##############
# DynamoDB Resources
##############

# DynamoDB Table for File Uploads
resource "aws_dynamodb_table" "file_uploads" {
  name           = var.dynamodb_table_name
  billing_mode   = var.dynamodb_billing_mode
  read_capacity  = var.dynamodb_billing_mode == "PROVISIONED" ? var.dynamodb_read_capacity : null
  write_capacity = var.dynamodb_billing_mode == "PROVISIONED" ? var.dynamodb_write_capacity : null
  hash_key       = "Id"

  attribute {
    name = "Id"
    type = "S"
  }

  # Optional: Add a GSI for querying by upload date
  global_secondary_index {
    name            = "UploadedAtIndex"
    hash_key        = "UploadedBy"
    range_key       = "UploadedAt"
    projection_type = "ALL"
    read_capacity   = var.dynamodb_billing_mode == "PROVISIONED" ? var.dynamodb_gsi_read_capacity : null
    write_capacity  = var.dynamodb_billing_mode == "PROVISIONED" ? var.dynamodb_gsi_write_capacity : null
  }

  # Optional: Add another GSI for querying by content type
  global_secondary_index {
    name            = "ContentTypeIndex"
    hash_key        = "ContentType"
    range_key       = "UploadedAt"
    projection_type = "KEYS_ONLY"
    read_capacity   = var.dynamodb_billing_mode == "PROVISIONED" ? var.dynamodb_gsi_read_capacity : null
    write_capacity  = var.dynamodb_billing_mode == "PROVISIONED" ? var.dynamodb_gsi_write_capacity : null
  }

  attribute {
    name = "UploadedBy"
    type = "S"
  }

  attribute {
    name = "UploadedAt"
    type = "S"
  }

  attribute {
    name = "ContentType"
    type = "S"
  }

  # Point-in-time recovery
  point_in_time_recovery {
    enabled = var.dynamodb_point_in_time_recovery
  }

  # Server-side encryption
  server_side_encryption {
    enabled = true
    # Note: kms_key_id is only available in newer versions of the AWS provider
    # For older versions, it will use AWS managed keys
  }

  # Deletion protection
  deletion_protection_enabled = var.dynamodb_deletion_protection

  # TTL for automatic cleanup of old files (optional)
  dynamic "ttl" {
    for_each = var.dynamodb_ttl_enabled ? [1] : []
    content {
      attribute_name = var.dynamodb_ttl_attribute
      enabled        = true
    }
  }

  tags = merge(var.tags, {
    Name        = var.dynamodb_table_name
    Description = "DynamoDB table for file upload storage and metadata"
    Service     = "FileUploader"
  })

  lifecycle {
    prevent_destroy = true
  }
}

# DynamoDB Table Policy (optional - for fine-grained access control)
resource "aws_dynamodb_resource_policy" "file_uploads_policy" {
  count        = var.enable_dynamodb_resource_policy ? 1 : 0
  resource_arn = aws_dynamodb_table.file_uploads.arn
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowECSTaskAccess"
        Effect = "Allow"
        Principal = {
          AWS = aws_iam_role.apprunner_instance_role.arn
        }
        Action = [
          "dynamodb:GetItem",
          "dynamodb:PutItem",
          "dynamodb:UpdateItem",
          "dynamodb:DeleteItem",
          "dynamodb:Query",
          "dynamodb:Scan"
        ]
        Resource = [
          aws_dynamodb_table.file_uploads.arn,
          "${aws_dynamodb_table.file_uploads.arn}/index/*"
        ]
      }
    ]
  })
}

##############
# CloudWatch Alarms for DynamoDB
##############

# Read throttle alarm
resource "aws_cloudwatch_metric_alarm" "dynamodb_read_throttles" {
  count = var.enable_dynamodb_monitoring ? 1 : 0

  alarm_name          = "${var.dynamodb_table_name}-read-throttles"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "UserErrors"
  namespace           = "AWS/DynamoDB"
  period              = "300"
  statistic           = "Sum"
  threshold           = "5"
  alarm_description   = "This metric monitors DynamoDB read throttles"
  alarm_actions       = var.sns_topic_arn != "" ? [var.sns_topic_arn] : []

  dimensions = {
    TableName = aws_dynamodb_table.file_uploads.name
  }

  tags = var.tags
}

# Write throttle alarm
resource "aws_cloudwatch_metric_alarm" "dynamodb_write_throttles" {
  count = var.enable_dynamodb_monitoring ? 1 : 0

  alarm_name          = "${var.dynamodb_table_name}-write-throttles"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "UserErrors"
  namespace           = "AWS/DynamoDB"
  period              = "300"
  statistic           = "Sum"
  threshold           = "5"
  alarm_description   = "This metric monitors DynamoDB write throttles"
  alarm_actions       = var.sns_topic_arn != "" ? [var.sns_topic_arn] : []

  dimensions = {
    TableName = aws_dynamodb_table.file_uploads.name
  }

  tags = var.tags
}

# High consumed read capacity alarm
resource "aws_cloudwatch_metric_alarm" "dynamodb_high_read_capacity" {
  count = var.enable_dynamodb_monitoring && var.dynamodb_billing_mode == "PROVISIONED" ? 1 : 0

  alarm_name          = "${var.dynamodb_table_name}-high-read-capacity"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "ConsumedReadCapacityUnits"
  namespace           = "AWS/DynamoDB"
  period              = "300"
  statistic           = "Average"
  threshold           = var.dynamodb_read_capacity * 0.8 # Alert at 80% utilization
  alarm_description   = "This metric monitors DynamoDB read capacity utilization"
  alarm_actions       = var.sns_topic_arn != "" ? [var.sns_topic_arn] : []

  dimensions = {
    TableName = aws_dynamodb_table.file_uploads.name
  }

  tags = var.tags
}

# High consumed write capacity alarm
resource "aws_cloudwatch_metric_alarm" "dynamodb_high_write_capacity" {
  count = var.enable_dynamodb_monitoring && var.dynamodb_billing_mode == "PROVISIONED" ? 1 : 0

  alarm_name          = "${var.dynamodb_table_name}-high-write-capacity"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "ConsumedWriteCapacityUnits"
  namespace           = "AWS/DynamoDB"
  period              = "300"
  statistic           = "Average"
  threshold           = var.dynamodb_write_capacity * 0.8 # Alert at 80% utilization
  alarm_description   = "This metric monitors DynamoDB write capacity utilization"
  alarm_actions       = var.sns_topic_arn != "" ? [var.sns_topic_arn] : []

  dimensions = {
    TableName = aws_dynamodb_table.file_uploads.name
  }

  tags = var.tags
}