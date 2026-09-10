using AegisRAG.Application.Abstractions;

namespace AegisRAG.Application.Documents.Process;

public sealed class ProcessDocumentHandler
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IDocumentTextExtractor _textExtractor;
    private readonly ITextChunker _textChunker;
    private readonly IEmbeddingService _embeddingService;

    public ProcessDocumentHandler(
        IDocumentRepository documentRepository,
        IFileStorage fileStorage,
        IDocumentTextExtractor textExtractor,
        ITextChunker textChunker,
        IEmbeddingService embeddingService)
    {
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
        _textExtractor = textExtractor;
        _textChunker = textChunker;
        _embeddingService = embeddingService;
    }

    public async Task HandleAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(
            documentId,
            cancellationToken);

        if (document is null)
            throw new InvalidOperationException(
                "Document not found.");

        try
        {
            document.MarkAsProcessing();

            await _documentRepository.UpdateAsync(
                document,
                cancellationToken);

            await using var stream =
                await _fileStorage.OpenReadAsync(
                    document.StoragePath,
                    cancellationToken);

            var pages = await _textExtractor.ExtractAsync(
                stream,
                document.ContentType,
                cancellationToken);

            var chunks = _textChunker.Chunk(pages);

            foreach (var chunk in chunks)
            {
                var embedding =
                    await _embeddingService.GenerateEmbeddingAsync(
                        chunk.Content,
                        cancellationToken);

                document.AddChunk(
                    chunk.Content,
                    chunk.ChunkIndex,
                    chunk.PageNumber,
                    embedding);
            }

            document.MarkAsCompleted();

            await _documentRepository.UpdateAsync(
                document,
                cancellationToken);
        }
        catch (Exception ex)
        {
            document.MarkAsFailed(
                ex.Message);

            await _documentRepository.UpdateAsync(
                document,
                cancellationToken);

            throw;
        }
    }
}