namespace Amora.Application.Abstractions;

public interface IStorageService
{
    // Hàm này trả về 2 link: 
    // 1. Link để App dùng để PUT file lên
    // 2. Link Public để lưu vào DB (dùng để nghe sau này)
    Task<(string UploadUrl, string PublicUrl)> GeneratePreSignedUploadUrlAsync(string fileExtension);
}
