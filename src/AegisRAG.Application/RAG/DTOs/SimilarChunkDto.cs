namespace AegisRAG.Application.RAG.DTOs;

public sealed record SimilarChunkDto(
    Guid ChunkId,
    Guid DocumentId,
    string Content,
    int ChunkIndex,
    int? PageNumber,
    double Similarity);