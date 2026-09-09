namespace AegisRAG.Application.RAG.Chat;

public sealed record RagSourceDto(
    Guid DocumentId,
    string FileName,
    Guid ChunkId,
    int ChunkIndex,
    int? PageNumber,
    double Similarity);