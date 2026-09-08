using System.Text;
using System.Text.Json;
using AegisRAG.Application.Abstractions;
using AegisRAG.Application.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AegisRAG.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IMessagePublisher
{
    private readonly RabbitMqOptions _options;

    public RabbitMqPublisher(
        IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task PublishAsync(
        DocumentUploadedMessage message,
        CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using var connection =
            await factory.CreateConnectionAsync(
                cancellationToken);

        await using var channel =
            await connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(message));

        var properties = new BasicProperties
        {
            Persistent = true
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}