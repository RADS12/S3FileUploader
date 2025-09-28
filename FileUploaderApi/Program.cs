using Amazon;
using Amazon.S3;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http.Features;
using FileUploaderApi.Services;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();
// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileUploaderApi", Version = "v1" });
});

// CORS (optional, open for testing)
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var awsRegion = builder.Configuration["AWS:Region"] ?? "us-east-2";

// AWS SDK clients (uses IAM role in AWS; local uses ~/.aws/credentials)
builder.Services.AddSingleton<IAmazonS3>(_ =>
    new AmazonS3Client(RegionEndpoint.GetBySystemName(awsRegion)));

// DynamoDB services
builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
    new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(awsRegion)));

builder.Services.AddSingleton<IDynamoDBContext>(provider =>
{
    var dynamoDbClient = provider.GetRequiredService<IAmazonDynamoDB>();
    return new DynamoDBContextBuilder().WithDynamoDBClient(() => dynamoDbClient).Build();
});

builder.Services.AddScoped<IDynamoFileService, DynamoFileService>();

// File upload size limit (optional, default is 128 MB)
builder.Services.Configure<FormOptions>(o =>
{
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartBodyLengthLimit = long.Parse(builder.Configuration["FileUpload:MaxSizeBytes"] ?? "524288000"); // 500 MB
    o.MemoryBufferThreshold = int.MaxValue;
});

var app = builder.Build();

//app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();