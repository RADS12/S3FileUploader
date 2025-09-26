##############
# S3 Bucket Reference
##############
data "aws_s3_bucket" "uploads" {
  bucket = var.bucket_name
}