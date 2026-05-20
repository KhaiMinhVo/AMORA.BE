using Amazon.S3;
using Amazon.S3.Model;
using Amora.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Amora.Infrastructure.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["Storage:BucketName"]
            ?? configuration["AWS:BucketName"]
            ?? "amora-voice-bucket";

        _publicBaseUrl = configuration["Storage:PublicBaseUrl"]?.TrimEnd('/')
            ?? $"https://{_bucketName}.s3.amazonaws.com";
    }

    public async Task<(string UploadUrl, string PublicUrl)> GeneratePreSignedUploadUrlAsync(string fileExtension)
    {
        return await GeneratePreSignedUploadUrlAsync(fileExtension, "voices");
    }

    public async Task<(string UploadUrl, string PublicUrl)> GeneratePreSignedUploadUrlAsync(string fileExtension, string? folder)
    {
        var safeFolder = string.IsNullOrWhiteSpace(folder) ? "files" : folder.Trim().Trim('/');
        var fileName = $"{safeFolder}/{Guid.NewGuid()}{fileExtension}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(5)
        };

        string uploadUrl = _s3Client.GetPreSignedURL(request);
        string publicUrl = $"{_publicBaseUrl}/{fileName}";

        return await Task.FromResult((uploadUrl, publicUrl));
    }
}
