using System.Text;
using System.Text.Json;
using Amora.Application.Features.Pets.Commands;
using Amora.Application.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Amora.Infrastructure.Messaging;

/// <summary>Lắng nghe queue chat_vibe_result từ Python worker.</summary>
public sealed class VibeResultConsumerService : BackgroundService
{
    private const string QueueName = "chat_vibe_result";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VibeResultConsumerService> _logger;
    private readonly string _amqpUrl;

    private IConnection? _connection;
    private IChannel? _channel;

    public VibeResultConsumerService(
        IServiceScopeFactory scopeFactory,
        ILogger<VibeResultConsumerService> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _amqpUrl = configuration["RabbitMQ:Url"] ?? "amqp://guest:guest@localhost:5672//";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_amqpUrl) };
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(0, 1, false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<ChatVibeResultMessage>(json, JsonOptions)
                    ?? throw new InvalidOperationException("Invalid vibe result payload.");

                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new UpdatePetAfterVoiceCommand(message), stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing vibe result.");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);
        _logger.LogInformation("VibeResultConsumer listening on {Queue}", QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
