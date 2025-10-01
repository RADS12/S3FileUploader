import sys
from awsglue.transforms import *
from awsglue.utils import getResolvedOptions
from pyspark.context import SparkContext
from awsglue.context import GlueContext
from awsglue.job import Job
from awsglue.dynamicframe import DynamicFrame
from pyspark.sql import functions as F
from pyspark.sql.types import *

# Get job parameters
args = getResolvedOptions(sys.argv, [
    'JOB_NAME',
    'source_database',
    'source_table', 
    'target_bucket',
    'target_prefix'
])

# Initialize Glue context
sc = SparkContext()
glueContext = GlueContext(sc)
spark = glueContext.spark_session
job = Job(glueContext)
job.init(args['JOB_NAME'], args)

print(f"Starting CSV to Parquet conversion for table: {args['source_table']}")

try:
    # Read data from Glue Catalog (discovered by crawler)
    dynamic_frame = glueContext.create_dynamic_frame.from_catalog(
        database=args['source_database'],
        table_name=args['source_table']
    )
    
    print(f"Source records count: {dynamic_frame.count()}")
    
    # Convert to Spark DataFrame for transformations
    df = dynamic_frame.toDF()
    
    # Data quality improvements and type conversions
    df_cleaned = df.select(
        F.col("orderid").cast(IntegerType()).alias("order_id"),
        F.col("customerid").alias("customer_id"),
        F.col("productname").alias("product_name"),
        F.col("category").alias("category"),
        F.col("quantity").cast(IntegerType()).alias("quantity"),
        F.col("unitprice").cast(DecimalType(10,2)).alias("unit_price"),
        F.to_date(F.col("orderdate"), "yyyy-MM-dd").alias("order_date"),
        F.to_date(F.col("shipdate"), "yyyy-MM-dd").alias("ship_date"),
        F.col("country").alias("country"),
        F.col("salesamount").cast(DecimalType(10,2)).alias("sales_amount")
    )
    
    # Add computed columns for analytics
    df_enhanced = df_cleaned.select(
        "*",
        F.year("order_date").alias("order_year"),
        F.month("order_date").alias("order_month"),
        F.dayofweek("order_date").alias("order_day_of_week"),
        (F.col("sales_amount") / F.col("quantity")).alias("avg_price_per_unit")
    )
    
    print(f"Processed records count: {df_enhanced.count()}")
    print("Sample of processed data:")
    df_enhanced.show(5)
    
    # Convert back to DynamicFrame
    dynamic_frame_final = DynamicFrame.fromDF(df_enhanced, glueContext, "final_frame")
    
    # Write as Parquet to S3 with partitioning by year and month for better query performance
    output_path = f"s3://{args['target_bucket']}/{args['target_prefix']}"
    
    glueContext.write_dynamic_frame.from_options(
        frame=dynamic_frame_final,
        connection_type="s3",
        connection_options={
            "path": output_path,
            "partitionKeys": ["order_year", "order_month"]
        },
        format="parquet",
        transformation_ctx="write_parquet"
    )
    
    print(f"Successfully wrote Parquet files to: {output_path}")
    print("Partitioned by: order_year, order_month")
    
except Exception as e:
    print(f"ETL Job failed with error: {str(e)}")
    raise e

finally:
    job.commit()
    print("ETL Job completed!")