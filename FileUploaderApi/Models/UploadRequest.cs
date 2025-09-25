// Models/UploadRequest.cs
using Microsoft.AspNetCore.Http;

namespace FileUploaderApi.Models;

public class UploadRequest
{
    // the form field name will be "file"
    public IFormFile File { get; set; } = default!;
    public string? KeyPrefix { get; set; }  // optional extra field
}
