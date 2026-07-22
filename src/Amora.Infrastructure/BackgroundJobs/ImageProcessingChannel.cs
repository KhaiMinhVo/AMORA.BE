using System.Threading.Channels;

namespace Amora.Infrastructure.BackgroundJobs;

public class ImageProcessingTask
{
    public string OriginalFileKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ImageType { get; set; } = "profile"; // "avatar", "profile", "chat"
}

public class ImageProcessingChannel
{
    private readonly Channel<ImageProcessingTask> _channel;

    public ImageProcessingChannel()
    {
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<ImageProcessingTask>(options);
    }

    public async ValueTask AddTaskAsync(ImageProcessingTask task, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(task, cancellationToken);
    }

    public IAsyncEnumerable<ImageProcessingTask> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
