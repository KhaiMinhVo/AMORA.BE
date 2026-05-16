namespace Amora.Application.Abstractions;

/// <summary>Publish JSON trực tiếp lên RabbitMQ queue (không qua Celery).</summary>
public interface IMessagePublisher
{
    Task PublishAsync<T>(string queueName, T payload, CancellationToken cancellationToken = default);
}
