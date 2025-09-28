# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file and project files
COPY S3FileUploader.sln ./
COPY FileUploaderApi/FileUploaderApi.csproj ./FileUploaderApi/
COPY FileUploaderApi.Tests/FileUploaderApi.Tests.csproj ./FileUploaderApi.Tests/

# Restore dependencies
RUN dotnet restore S3FileUploader.sln

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR /src/FileUploaderApi
RUN dotnet build FileUploaderApi.csproj -c Release -o /app/build

# Publish the application
RUN dotnet publish FileUploaderApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Copy the published application
COPY --from=build /app/publish .

# Expose port 8080 (non-privileged port)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
#ENV ASPNETCORE_ENVIRONMENT=Production

# DynamoDB Configuration (can be overridden at runtime)
#ENV DYNAMODB__TABLENAME=FileUploads
#ENV DYNAMODB__REGION=us-east-2

# AWS Configuration (provide at runtime for security)
#ENV AWS_DEFAULT_REGION=us-east-2
#ENV AWS_ACCESS_KEY_ID=
#ENV AWS_SECRET_ACCESS_KEY=

# Health check
#HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
# CMD curl -f http://localhost:8080/weatherforecast || exit 1

# Start the application
ENTRYPOINT ["dotnet", "FileUploaderApi.dll"]