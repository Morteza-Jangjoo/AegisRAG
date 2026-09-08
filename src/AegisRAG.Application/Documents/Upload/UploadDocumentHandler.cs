using AegisRAG.Application.Abstractions;
using AegisRAG.Application.Messaging;
using AegisRAG.Domain.Entities;

namespace AegisRAG.Application.Documents.Upload;

public sealed class UploadDocumentHandler
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IMessagePublisher _messagePublisher;

    public UploadDocumentHandler(
        IDocumentRepository documentRepository,
        IFileStorage fileStorage,
        IMessagePublisher messagePublisher)
    {
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
        _messagePublisher = messagePublisher;
    }

    public async Task<UploadDocumentResult> HandleAsync(
        UploadDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.FileSize <= 0)
            throw new ArgumentException(
                "File cannot be empty.");

        if (string.IsNullOrWhiteSpace(command.FileName))
            throw new ArgumentException(
                "File name is required.");

        var storagePath =
            await _fileStorage.SaveAsync(
                command.Content,
                command.FileName,
                cancellationToken);

        var document = new Document(
            command.FileName,
            command.ContentType,
            command.FileSize,
            storagePath);

        await _documentRepository.AddAsync(
            document,
            cancellationToken);

        await _messagePublisher.PublishAsync(
            new DocumentUploadedMessage(
                document.Id),
            cancellationToken);

        return new UploadDocumentResult(
            document.Id,
            document.FileName,
            document.Status.ToString());
    }
}