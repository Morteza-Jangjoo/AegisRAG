using AegisRAG.Application.Abstractions;

namespace AegisRAG.Application.Documents.GetStatus;

public sealed class GetDocumentStatusHandler
{
    private readonly IDocumentRepository _documentRepository;

    public GetDocumentStatusHandler(
        IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<DocumentStatusResult?> HandleAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document =
            await _documentRepository.GetByIdAsync(
                documentId,
                cancellationToken);

        if (document is null)
            return null;

        return new DocumentStatusResult(
            document.Id,
            document.FileName,
            document.Status,
            document.ErrorMessage,
            document.CreatedAtUtc,
            document.ProcessedAtUtc,
            document.Chunks.Count);
    }
}