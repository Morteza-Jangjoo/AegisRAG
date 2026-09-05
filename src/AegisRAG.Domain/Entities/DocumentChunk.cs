namespace AegisRAG.Domain.Entities;

public class DocumentChunk
{
    private DocumentChunk()
    {
    }

    public DocumentChunk(
        Guid documentId,
        string content,
        int chunkIndex,
        int? pageNumber)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        Content = content;
        ChunkIndex = chunkIndex;
        PageNumber = pageNumber;
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public string Content { get; private set; } = null!;

    public int ChunkIndex { get; private set; }

    public int? PageNumber { get; private set; }

    public Document Document { get; private set; } = null!;
}