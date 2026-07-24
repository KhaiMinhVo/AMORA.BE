using System.Text;
using System.Text.Json;
using Amora.Application.Abstractions;
using RabbitMQ.Client;

namespace Amora.Infrastructure.Messaging;

/// <summary>
/// Publish Celery-compatible task messages lên RabbitMQ.
/// Format tuân theo Celery Message Protocol v1 (JSON serializer).
/// </summary>
public sealed class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly SemaphoreSlim _publishGate = new(1, 1);

    private RabbitMqMessageBus(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    /// <summary>Factory method async để khởi tạo connection.</summary>
    public static async Task<RabbitMqMessageBus> CreateAsync(string amqpUrl)
    {
        var factory = new ConnectionFactory { Uri = new Uri(amqpUrl) };
        IConnection? connection = null;
        IChannel? channel = null;

        for (int i = 0; i < 5; i++)
        {
            try
            {
                connection = await factory.CreateConnectionAsync();
                channel = await connection.CreateChannelAsync(new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true));
                break;
            }
            catch (Exception)
            {
                if (i == 4) throw;
                await Task.Delay(2000);
            }
        }

        // Đảm bảo queue "celery" tồn tại, durable để không mất khi restart RabbitMQ
        await channel!.QueueDeclareAsync(
            queue: "celery",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        return new RabbitMqMessageBus(connection!, channel);
    }

    public async Task PublishAsync(string taskName, object[] args, CancellationToken cancellationToken = default)
    {
        // Celery v1 message envelope
        var envelope = new
        {
            id = Guid.NewGuid().ToString(),
            task = taskName,
            args,
            kwargs = new { },
            retries = 0,
            eta = (string?)null,
            expires = (string?)null,
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));

        var props = new BasicProperties
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            DeliveryMode = DeliveryModes.Persistent, // Tin nhắn bền vững, không mất khi broker restart
        };

        await _publishGate.WaitAsync(cancellationToken);
        try
        {
            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: "celery",
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken
            );
        }
        finally
        {
            _publishGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
        _publishGate.Dispose();
    }
}
