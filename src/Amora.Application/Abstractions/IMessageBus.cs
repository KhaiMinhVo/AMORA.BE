namespace Amora.Application.Abstractions;

/// <summary>
/// Abstraction cho Message Queue (RabbitMQ). Tầng Application không biết broker cụ thể là gì.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publish một task lên queue để Python Worker xử lý.
    /// </summary>
    /// <param name="taskName">Tên task Celery, ví dụ: "tasks.process_voice_post"</param>
    /// <param name="args">Danh sách argument truyền vào task theo thứ tự.</param>
    Task PublishAsync(string taskName, object[] args, CancellationToken cancellationToken = default);
}
