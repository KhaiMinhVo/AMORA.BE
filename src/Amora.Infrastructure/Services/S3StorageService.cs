using Amazon.S3;
using Amazon.S3.Model;
using Amora.Application.Abstractions;

namespace Amora.Infrastructure.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3StorageService(IAmazonS3 s3Client, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:BucketName"] ?? "amora-voice-bucket";
    }

    public async Task<(string UploadUrl, string PublicUrl)> GeneratePreSignedUploadUrlAsync(string fileExtension)
    {
        // 1. Tạo tên file ngẫu nhiên (tránh trùng lặp)
        var fileName = $"voices/{Guid.NewGuid()}{fileExtension}";

        // 2. Cấu hình request cấp quyền
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            Verb = HttpVerb.PUT, // Bắt buộc Client phải dùng HTTP PUT
            Expires = DateTime.UtcNow.AddMinutes(5) // Link chỉ có tác dụng trong 5 phút
        };

        // 3. Lấy URL upload
        string uploadUrl = _s3Client.GetPreSignedURL(request);
        
        // 4. Tạo URL public để lưu DB
        string publicUrl = $"https://{_bucketName}.s3.amazonaws.com/{fileName}";

        return await Task.FromResult((uploadUrl, publicUrl)); // Awaiting Task.FromResult to satisfy async method
    }
}
