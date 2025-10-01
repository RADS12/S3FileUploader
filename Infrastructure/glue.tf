##############
# AWS Glue Resources
##############

# Glue Database for File Processing
resource "aws_glue_catalog_database" "file_processing_db" {
  name        = "${var.tags.Project}-file-processing"
  description = "Database for file upload processing and analytics"
  
  tags = var.tags
}

# S3 Bucket for Processed Data
resource "aws_s3_bucket" "processed_data" {
  bucket = "${var.bucket_name}-processed"
  tags = merge(var.tags, {
    Purpose = "Processed data storage"
  })
}

resource "aws_s3_bucket_versioning" "processed_data" {
  bucket = aws_s3_bucket.processed_data.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "processed_data" {
  bucket = aws_s3_bucket.processed_data.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# S3 Bucket for Glue Scripts
resource "aws_s3_bucket" "glue_scripts" {
  bucket = "${var.bucket_name}-glue-scripts"
  tags = merge(var.tags, {
    Purpose = "Glue ETL scripts storage"
  })
}

resource "aws_s3_bucket_versioning" "glue_scripts" {
  bucket = aws_s3_bucket.glue_scripts.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "glue_scripts" {
  bucket = aws_s3_bucket.glue_scripts.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# Glue Crawler for Raw Files
resource "aws_glue_crawler" "raw_files_crawler" {
  count = var.enable_glue_integration ? 1 : 0
  
  database_name = aws_glue_catalog_database.file_processing_db.name
  name          = "${var.tags.Project}-raw-files-crawler"
  role          = aws_iam_role.glue_service_role.arn
  
  # Scan both uploads folder and ForGlue folder
  s3_target {
    path = "s3://${data.aws_s3_bucket.uploads.bucket}/uploads/"
  }
  
  s3_target {
    path = "s3://${data.aws_s3_bucket.uploads.bucket}/ForGlue/"
  }
  
  configuration = jsonencode({
    CrawlerOutput = {
      Partitions = { AddOrUpdateBehavior = "InheritFromTable" }
    }
    Version = 1.0
  })
  
  schedule = var.glue_crawler_schedule
  
  tags = var.tags
}

# Glue Job for CSV Processing
resource "aws_glue_job" "csv_processor" {
  count = var.enable_glue_csv_processing ? 1 : 0
  
  name         = "${var.tags.Project}-csv-processor"
  role_arn     = aws_iam_role.glue_service_role.arn
  glue_version = var.glue_version
  
  command {
    script_location = "s3://${aws_s3_bucket.glue_scripts.bucket}/csv_processor.py"
    python_version  = "3"
  }
  
  default_arguments = {
    "--job-language"                     = "python"
    "--job-bookmark-option"              = "job-bookmark-enable"
    "--enable-metrics"                   = ""
    "--enable-continuous-cloudwatch-log" = "true"
    "--source-bucket"                    = data.aws_s3_bucket.uploads.bucket
    "--target-bucket"                    = aws_s3_bucket.processed_data.bucket
    "--dynamodb-table"                   = var.dynamodb_table_name
  }
  
  execution_property {
    max_concurrent_runs = var.glue_max_concurrent_runs
  }
  
  worker_type       = var.glue_worker_type
  number_of_workers = var.glue_number_of_workers
  
  tags = var.tags
}

# Glue Job for JSON Processing
resource "aws_glue_job" "json_processor" {
  count = var.enable_glue_json_processing ? 1 : 0
  
  name         = "${var.tags.Project}-json-processor"
  role_arn     = aws_iam_role.glue_service_role.arn
  glue_version = var.glue_version
  
  command {
    script_location = "s3://${aws_s3_bucket.glue_scripts.bucket}/json_processor.py"
    python_version  = "3"
  }
  
  default_arguments = {
    "--job-language"                     = "python"
    "--job-bookmark-option"              = "job-bookmark-enable"
    "--enable-metrics"                   = ""
    "--enable-continuous-cloudwatch-log" = "true"
    "--source-bucket"                    = data.aws_s3_bucket.uploads.bucket
    "--target-bucket"                    = aws_s3_bucket.processed_data.bucket
    "--dynamodb-table"                   = var.dynamodb_table_name
  }
  
  execution_property {
    max_concurrent_runs = var.glue_max_concurrent_runs
  }
  
  worker_type       = var.glue_worker_type
  number_of_workers = var.glue_number_of_workers
  
  tags = var.tags
}

# Note: Data sources for VPC and subnets are already defined in ecs-fargate.tf
# We'll reuse those existing data sources if needed for Glue VPC connections