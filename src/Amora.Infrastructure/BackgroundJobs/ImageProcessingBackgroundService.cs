using Amora.Application.Abstractions;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amora.Infrastructure.BackgroundJobs;

public class ImageProcessingBackgroundService : BackgroundService
{
    private readonly ImageProcessingChannel _channel;
    private readonly ILogger<ImageProcessingBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ImageProcessingBackgroundService(
        ImageProcessingChannel channel, 
        ILogger<ImageProcessingBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _channel = channel;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Image Processing Background Service is starting.");

        await foreach (var task in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessImageAsync(task, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image {FileKey} for user {UserId}", task.OriginalFileKey, task.UserId);
            }
        }
    }

    private async Task ProcessImageAsync(ImageProcessingTask task, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var realtimeNotifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();

        _logger.LogInformation("Downloading {FileKey} for processing...", task.OriginalFileKey);
        
        // 1. Download file from S3
        using var rawStream = await storageService.DownloadFileAsync(task.OriginalFileKey, stoppingToken);

        // 2. Process with Magick.NET
        // Magick.NET will automatically detect format and throw if invalid
        using var image = new MagickImage(rawStream);
        
        // AutoOrient (apply EXIF rotation)
        image.AutoOrient();
        
        // Strip metadata (including EXIF GPS)
        image.Strip();

        // Resize based on type
        uint maxSize = task.ImageType.ToLowerInvariant() == "avatar" ? 1024u : 1920u;
        if (image.Width > maxSize || image.Height > maxSize)
        {
            var size = new MagickGeometry(maxSize, maxSize)
            {
                IgnoreAspectRatio = false
            };
            image.Resize(size);
        }

        // Convert to WebP
        image.Format = MagickFormat.WebP;
        image.Quality = 75;
        // Optimize WebP encoding speed (0 = fastest, 6 = slowest, 4 = default)
        image.Settings.SetDefine(MagickFormat.WebP, "method", "0");

        using var outStream = new MemoryStream();
        image.Write(outStream);
        outStream.Position = 0;

        // 3. Upload processed image
        string extension = ".webp";
        string mimeType = "image/webp";
        var processedUrl = await storageService.UploadFileAsync(outStream, extension, "processed-images", mimeType);

        // 4. Delete raw file
        await storageService.DeleteFileAsync(task.OriginalFileKey, stoppingToken);

        _logger.LogInformation("Successfully processed and saved to {Url}", processedUrl);

        // 5. Notify frontend via SignalR
        if (Guid.TryParse(task.UserId, out var userIdGuid))
        {
            await realtimeNotifier.NotifyImageProcessedAsync(userIdGuid, processedUrl, task.ImageType, stoppingToken);
        }
    }
}
