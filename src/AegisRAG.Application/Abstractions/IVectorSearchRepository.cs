using AegisRAG.Application.RAG.DTOs;

namespace AegisRAG.Application.Abstractions;

public interface IVectorSearchRepository
{
    Task<IReadOnlyList<SimilarChunkDto>> SearchAsync(
        float[] embedding,
        int topK,
        CancellationToken cancellationToken = default);
}