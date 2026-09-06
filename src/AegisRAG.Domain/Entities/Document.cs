using AegisRAG.Domain.Enums;

namespace AegisRAG.Domain.Entities;

public class Document
{
    private readonly List<DocumentChunk> _chunks = [];

    private Document()
    {
    }

    public Document(
        string fileName,
        string contentType,
        long fileSize,
        string storagePath)
    {
        Id = Guid.NewGuid();
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        StoragePath = storagePath;
        Status = DocumentStatus.Uploaded;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long FileSize { get; private set; }

    public string StoragePath { get; private set; } = null!;

    public DocumentStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public string? ErrorMessage { get; private set; }

    public IReadOnlyCollection<DocumentChunk> Chunks => _chunks.AsReadOnly();

    public void MarkAsProcessing()
    {
        Status = DocumentStatus.Processing;
        ErrorMessage = null;
    }

    public void MarkAsCompleted()
    {
        Status = DocumentStatus.Completed;
        ProcessedAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = DocumentStatus.Failed;
        ErrorMessage = errorMessage;
    }
    public void AddChunk(
    string content,
    int chunkIndex,
    int? pageNumber,
    float[]? embedding = null)
    {
        var chunk = new DocumentChunk(
            Id,
            content,
            chunkIndex,
            pageNumber);

        if (embedding is not null)
            chunk.SetEmbedding(embedding);

        _chunks.Add(chunk);
    }
}