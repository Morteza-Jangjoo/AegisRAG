using AegisRAG.Application.Abstractions;
using AegisRAG.Domain.Entities;
using AegisRAG.Infrastructure.Documents;
using AegisRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AegisRAG.IntegrationTests.Evaluation;

public sealed class EvaluationDataSeeder
{

    private readonly AegisRagDbContext _dbContext;
    private readonly IDocumentTextExtractor _textExtractor;
    private readonly IEmbeddingService _embeddingService;

    public EvaluationDataSeeder(
        AegisRagDbContext dbContext,
        IDocumentTextExtractor textExtractor,
        IEmbeddingService embeddingService)
    {
        _dbContext = dbContext;
        _textExtractor = textExtractor;
        _embeddingService = embeddingService;
    }

    public async Task SeedAsync(
    string filePath,
    int chunkSize,
    int overlap,
    CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Evaluation document was not found.",
                filePath);
        }

        await _dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE "DocumentChunks", "Documents" CASCADE;
            """,
            cancellationToken);

        await using var stream =
            File.OpenRead(filePath);

        var pages =
            await _textExtractor.ExtractAsync(
                stream,
                "text/plain",
                cancellationToken);

        var chunker = new TextChunker(
            chunkSize,
            overlap);

        var chunks = chunker.Chunk(pages);

        var fileInfo = new FileInfo(filePath);

        var document = new Document(
            fileInfo.Name,
            "text/plain",
            fileInfo.Length,
            filePath);

        document.MarkAsProcessing();

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

        _dbContext.Documents.Add(document);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}