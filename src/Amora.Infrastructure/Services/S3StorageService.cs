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
            Expires = DateTime.UtcNow.AddMinutes(5),
            Protocol = _publicBaseUrl.StartsWith("http://") ? Protocol.HTTP : Protocol.HTTPS
        };

        string uploadUrl = _s3Client.GetPreSignedURL(request);
        string publicUrl = $"{_publicBaseUrl}/{fileName}";

        return await Task.FromResult((uploadUrl, publicUrl));
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileExtension, string? folder, string? contentType = null)
    {
        var safeFolder = string.IsNullOrWhiteSpace(folder) ? "files" : folder.Trim().Trim('/');
        var fileName = $"{safeFolder}/{Guid.NewGuid()}{fileExtension}";

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            InputStream = fileStream,
            ContentType = contentType ?? "application/octet-stream"
        };

        await _s3Client.PutObjectAsync(putRequest);

        return $"{_publicBaseUrl}/{fileName}";
    }
    public async Task<Stream> DownloadFileAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var response = await _s3Client.GetObjectAsync(_bucketName, fileKey, cancellationToken);
        var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteFileAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        await _s3Client.DeleteObjectAsync(_bucketName, fileKey, cancellationToken);
    }

    public async Task<long> GetTotalStorageSizeAsync(CancellationToken cancellationToken = default)
    {
        long totalBytes = 0;
        var listObjectsRequest = new ListObjectsV2Request
        {
            BucketName = _bucketName
        };
        
        do
        {
            var listObjectsResponse = await _s3Client.ListObjectsV2Async(listObjectsRequest, cancellationToken);
            totalBytes += listObjectsResponse.S3Objects.Sum(o => o.Size);
            listObjectsRequest.ContinuationToken = listObjectsResponse.NextContinuationToken;
        } while (!string.IsNullOrEmpty(listObjectsRequest.ContinuationToken));

        return totalBytes;
    }

    public async Task<(long Size, DateTimeOffset? LastModified)> GetLatestBackupInfoAsync(CancellationToken cancellationToken = default)
    {
        var backupRequest = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = "backups/"
        };
        
        var backupsResponse = await _s3Client.ListObjectsV2Async(backupRequest, cancellationToken);
        var latestBackup = backupsResponse.S3Objects.OrderByDescending(o => o.LastModified).FirstOrDefault();
        
        if (latestBackup != null)
        {
            return (latestBackup.Size, latestBackup.LastModified);
        }

        return (0, null);
    }
}
