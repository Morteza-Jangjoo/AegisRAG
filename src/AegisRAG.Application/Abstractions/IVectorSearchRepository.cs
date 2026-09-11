using AegisRAG.Application.RAG.DTOs;

namespace AegisRAG.Application.Abstractions;

public interface IVectorSearchRepository
{
    Task<IReadOnlyList<SimilarChunkDto>> SearchAsync(
    float[] embedding,
    int topK,
    double minimumSimilarity = 0.40,
    CancellationToken cancellationToken = default);
}