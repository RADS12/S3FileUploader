# AWS Glue ETL Integration Guide

## 🎯 **What is AWS Glue?**

AWS Glue is a serverless data integration service that helps you:

- **Discover** and catalog your data automatically
- **Transform** data between different formats (CSV → Parquet, JSON cleanup)
- **Load** data into data lakes, warehouses, or analytics services
- **Run ETL jobs** on a schedule or triggered by file uploads

## 🏗️ **How Glue Fits Your File Upload Solution**

### **Current Architecture**

```
User → Upload File → S3 Storage → DynamoDB Metadata
```

### **With Glue Integration**

```
User → Upload File → S3 Storage → DynamoDB Metadata
                         ↓
                   Glue Crawler (discovers schema)
                         ↓
                   Glue ETL Job (processes file)
                         ↓
                   Analytics-Ready Data → Query with Athena
```

## 🎯 **Three Main Integration Approaches**

### **Option 1: File Processing Pipeline (Recommended)**

- **What**: Process uploaded files into analytics-ready formats
- **Example**: Convert CSV files to Parquet, clean JSON data
- **Benefit**: Better performance for analytics, data compression

### **Option 2: Data Catalog & Discovery**

- **What**: Automatically discover schemas of uploaded files
- **Example**: Catalog all CSV columns, JSON structures
- **Benefit**: Query files directly with SQL using Athena

### **Option 3: Real-time Processing**

- **What**: Process files immediately after upload
- **Example**: File upload triggers Lambda, which starts Glue job
- **Benefit**: Near real-time data processing

## 📋 **Learning Path (Recommended Order)**

### **Week 1: Glue Basics**

1. Explore AWS Glue Console
2. Create simple crawler on existing S3 files
3. Query discovered data with Athena

### **Week 2: ETL Jobs**

1. Create basic Glue job (Python)
2. Transform one file type (CSV → Parquet)
3. Monitor job execution in CloudWatch

### **Week 3: Integration**

1. Add Glue to Terraform infrastructure
2. Integrate Glue API calls in .NET application
3. Set up automated file processing

---

## 🛠️ **Terraform Infrastructure Changes**

### **What We'll Add**

- **4 new files** to your Infrastructure folder
- **Glue Database** for file cataloging
- **Glue Crawler** to discover file schemas
- **ETL Jobs** for file processing
- **IAM Roles** for Glue permissions

### **New Files Overview**

#### **1. `glue.tf` - Main Glue Resources**

- Glue Database for organizing your data
- Glue Crawler to scan uploaded files
- ETL Jobs for processing (CSV → Parquet)
- S3 buckets for processed data and scripts

#### **2. `glue_iam.tf` - Permissions**

- IAM role for Glue service
- S3 access permissions
- DynamoDB access permissions
- CloudWatch logging permissions

#### **3. Updates to `variables.tf`**

- Glue configuration options
- Enable/disable features
- Scheduling options
- Resource sizing

#### **4. Updates to `outputs.tf`**

- Glue resource names and ARNs
- S3 bucket names for processed data
- Database and crawler information

### **Configuration Options**

```hcl
# Enable/disable Glue features
enable_glue_integration = true
enable_csv_processing = true

# Scheduling
glue_crawler_schedule = "cron(0 12 * * ? *)"  # Daily at noon

# Resource sizing
glue_worker_type = "G.1X"
glue_number_of_workers = 2
```

## 💰 **Cost Estimate**

### **Monthly Costs (Typical Usage)**

- **Glue Crawler**: ~$13/month (daily runs)
- **ETL Jobs**: ~$5-20/month (depends on file volume)
- **Additional S3**: ~$2-5/month (processed data storage)
- **Total**: ~$20-40/month

### **Cost Control**

- Start with minimal settings (G.1X workers, 2 workers max)
- Use scheduled crawlers (not continuous)
- Enable job bookmarks to avoid reprocessing

## 🎯 **Real-World Example**

### **Scenario: CSV File Processing**

```
1. User uploads sales_data.csv → Your API
2. File stored in S3 → Metadata in DynamoDB
3. S3 event triggers → Glue Crawler (discovers: Date, Product, Amount columns)
4. Crawler updates → Data Catalog with schema
5. Glue ETL job → Converts CSV to Parquet (faster queries)
6. Processed file → Stored in analytics S3 bucket
7. You can now → Query with Athena: "SELECT SUM(Amount) FROM sales_data"
```

## 🚀 **What You'll Be Able To Do**

### **Immediate Benefits**

- ✅ **Query uploaded files with SQL** (via Athena)
- ✅ **Automatic schema discovery** for any file format
- ✅ **Convert files to efficient formats** (Parquet)
- ✅ **Track data lineage** and processing history

### **Advanced Capabilities**

- ✅ **Connect to QuickSight** for dashboards
- ✅ **Build data pipelines** with multiple processing steps
- ✅ **Integration with other AWS services** (Redshift, EMR)
- ✅ **Machine learning data preparation**

## � **Implementation Complete!**

---

### **✅ Terraform Files Created**

The following infrastructure files have been created:

#### **1. `Infrastructure/glue.tf` (Main Resources)**

- **Glue Database**: `file-uploader-api-file-processing`
- **S3 Buckets**:
  - `rad-s3-demo-first-1-processed` (processed data)
  - `rad-s3-demo-first-1-glue-scripts` (ETL scripts)
- **Glue Crawler**: Discovers schemas daily at noon
- **ETL Jobs**:
  - CSV processor (converts CSV → Parquet)
  - JSON processor (optional, disabled by default)

#### **2. `Infrastructure/glue_iam.tf` (Security)**

- **IAM Role**: `file-uploader-api-glue-service-role`
- **Policies**: S3, DynamoDB, CloudWatch, Glue Catalog access
- **Permissions**: Least-privilege access for Glue operations

#### **3. `Infrastructure/variables.tf` (Updated)**

```hcl
# Key Glue Configuration Variables Added:
enable_glue_integration = true          # Master switch
enable_glue_csv_processing = true       # CSV → Parquet jobs
enable_glue_json_processing = false     # JSON processing (optional)
glue_worker_type = "G.1X"              # Cost-effective workers
glue_number_of_workers = 2             # Minimal concurrency
glue_crawler_schedule = "cron(0 12 * * ? *)"  # Daily at noon
```

#### **4. `Infrastructure/outputs.tf` (Updated)**

- Glue database and crawler names
- S3 bucket names for processed data
- IAM role ARNs
- Job names and configurations

### **💰 Cost-Optimized Configuration**

The implementation uses cost-conscious defaults:

- **G.1X Workers**: Smallest instance type ($0.44/DPU-hour)
- **2 Workers**: Minimal parallel processing
- **Daily Schedule**: Crawler runs once per day
- **Job Bookmarks**: Avoid reprocessing same files

**Estimated Monthly Cost: $20-40** (depending on usage)

## 📝 **Next Steps**

1. ✅ **Infrastructure files created** - Ready for deployment
2. **Deploy with Terraform**:
   ```bash
   cd Infrastructure
   terraform plan    # Review changes
   terraform apply   # Deploy Glue resources
   ```
3. **Upload sample files** and test automated processing
4. **Monitor in AWS Console**: Glue, Athena, CloudWatch
5. **Create ETL scripts** for your specific file types

## �️ **Terraform Deployment Commands**

### **Deploy the Infrastructure**

```bash
# Navigate to Infrastructure folder
cd Infrastructure

# Review what will be created
terraform plan

# Deploy Glue resources
terraform apply

# Check deployment results
terraform output
```

### **Expected Outputs After Deployment**

```
glue_database_name = "file-uploader-api-file-processing"
glue_crawler_name = "file-uploader-api-raw-files-crawler"
glue_processed_data_bucket = "rad-s3-demo-first-1-processed"
glue_scripts_bucket = "rad-s3-demo-first-1-glue-scripts"
glue_service_role_arn = "arn:aws:iam::123456789:role/file-uploader-api-glue-service-role"
glue_csv_job_name = "file-uploader-api-csv-processor"
```

### **Verify Deployment**

```bash
# Check Glue database exists
aws glue get-database --name file-uploader-api-file-processing

# List crawlers
aws glue get-crawlers --query 'Crawlers[?Name==`file-uploader-api-raw-files-crawler`]'

# Check S3 buckets created
aws s3 ls | grep processed
aws s3 ls | grep glue-scripts
```

## 🔗 **Useful Resources**

- **AWS Glue Console**: https://console.aws.amazon.com/glue/home
- **Athena Console**: https://console.aws.amazon.com/athena/home
- **CloudWatch**: https://console.aws.amazon.com/cloudwatch/home
- **S3 Console**: https://console.aws.amazon.com/s3/home

## 📋 **Configuration Reference**

### **Enable/Disable Features**

```hcl
# In terraform.tfvars or as -var flags:
enable_glue_integration = true     # Master switch
enable_glue_csv_processing = true  # CSV jobs
enable_glue_json_processing = false # JSON jobs (optional)
```

### **Adjust Resources**

```hcl
glue_worker_type = "G.2X"          # Upgrade to larger workers
glue_number_of_workers = 5         # More parallel processing
glue_crawler_schedule = "cron(0 */6 * * ? *)" # Every 6 hours
```

---

**🎉 AWS Glue ETL Integration Complete!**

_Your file upload solution now has powerful analytics and processing capabilities._

---

_Last Updated: September 30, 2025_
