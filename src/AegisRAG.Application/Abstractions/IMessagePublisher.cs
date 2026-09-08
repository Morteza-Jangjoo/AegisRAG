using AegisRAG.Application.Messaging;

namespace AegisRAG.Application.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync(
        DocumentUploadedMessage message,
        CancellationToken cancellationToken = default);
}