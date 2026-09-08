using System.Text;
using System.Text.Json;
using AegisRAG.Application.Documents.Process;
using AegisRAG.Application.Messaging;
using AegisRAG.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AegisRAG.Worker.Services;

public sealed class DocumentProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    public DocumentProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection =
            await factory.CreateConnectionAsync(
                stoppingToken);

        _channel =
            await _connection.CreateChannelAsync(
                cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer =
            new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                Console.WriteLine(
                    $"[RabbitMQ] Message received. DeliveryTag: {args.DeliveryTag}");

                var json =
                    Encoding.UTF8.GetString(
                        args.Body.ToArray());

                Console.WriteLine(
                    $"[RabbitMQ] Message body: {json}");

                var message =
                    JsonSerializer.Deserialize<
                        DocumentUploadedMessage>(json);

                if (message is null)
                    throw new InvalidOperationException(
                        "Invalid document message.");

                Console.WriteLine(
                    $"[Worker] Processing document: {message.DocumentId}");

                using var scope =
                    _scopeFactory.CreateScope();

                var handler =
                    scope.ServiceProvider
                        .GetRequiredService<
                            ProcessDocumentHandler>();
                
                Console.WriteLine(
                    $"[Worker] ProcessDocumentHandler resolved.");

                await handler.HandleAsync(
                    message.DocumentId,
                    stoppingToken);

                 Console.WriteLine(
                    $"[Worker] Document processed successfully: {message.DocumentId}");

                await _channel.BasicAckAsync(
                    args.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);

                Console.WriteLine(
                $"[RabbitMQ] Message ACKed. DeliveryTag: {args.DeliveryTag}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Document processing failed: {ex}");

                await _channel.BasicNackAsync(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        Console.WriteLine(
            $"Worker listening on queue: {_options.QueueName}");

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(
                cancellationToken);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(
                cancellationToken);
        }

        await base.StopAsync(
            cancellationToken);
    }
}