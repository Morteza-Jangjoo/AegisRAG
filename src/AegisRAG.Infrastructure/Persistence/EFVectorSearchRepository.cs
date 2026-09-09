using AegisRAG.Application.Abstractions;
using AegisRAG.Application.RAG.DTOs;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AegisRAG.Infrastructure.Persistence;

public sealed class EFVectorSearchRepository
    : IVectorSearchRepository
{
    private readonly AegisRagDbContext _dbContext;

    public EFVectorSearchRepository(
        AegisRagDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SimilarChunkDto>> SearchAsync(
        float[] embedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var queryVector = new Vector(embedding);

        var results = await _dbContext.DocumentChunks
            .Where(x => x.Embedding != null)
            .OrderBy(x =>
                x.Embedding!.CosineDistance(queryVector))
            .Take(topK)
            .Select(x => new SimilarChunkDto(
            x.Id,
            x.DocumentId,
            x.Document.FileName,
            x.Content,
            x.ChunkIndex,
            x.PageNumber,
            1 - x.Embedding!
                .CosineDistance(queryVector)))
            .ToListAsync(cancellationToken);

        return results;
    }
}