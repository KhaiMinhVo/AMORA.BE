using System.Text;
using System.Text.Json;
using Amora.Application.Abstractions;
using RabbitMQ.Client;

namespace Amora.Infrastructure.Messaging;

public sealed class RabbitMqMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private RabbitMqMessagePublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<RabbitMqMessagePublisher> CreateAsync(string amqpUrl)
    {
        var factory = new ConnectionFactory { Uri = new Uri(amqpUrl) };
        IConnection? connection = null;
        IChannel? channel = null;

        for (int i = 0; i < 5; i++)
        {
            try
            {
                connection = await factory.CreateConnectionAsync();
                channel = await connection.CreateChannelAsync();
                break;
            }
            catch (Exception)
            {
                if (i == 4) throw;
                await Task.Delay(2000);
            }
        }

        // Đảm bảo exchange "chat_vibe_commands" tồn tại
        await channel!.ExchangeDeclareAsync(
            exchange: "chat_vibe_commands",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null
        );

        return new RabbitMqMessagePublisher(connection!, channel);
    }

    public async Task PublishAsync<T>(string queueName, T payload, CancellationToken cancellationToken = default)
    {
        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel.BasicPublishAsync(string.Empty, queueName, false, props, body, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
