using AegisRAG.Domain.Entities;
using AegisRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace AegisRAG.IntegrationTests.Infrastructure;

public class SemanticSearchRepositoryTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=aegisrag_test;Username=postgres;Password=postgres";

    [Fact]
    public async Task SearchAsync_ShouldReturnRelevantChunksAboveThreshold()
    {
        var options =
            new DbContextOptionsBuilder<AegisRagDbContext>()
                .UseNpgsql(
                    ConnectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.UseVector();
                    })
                .Options;

        await using var db =
            new AegisRagDbContext(options);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE EXTENSION IF NOT EXISTS vector;");

        await db.Database.MigrateAsync();

        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE "DocumentChunks", "Documents" CASCADE;
            """);

        var document = new Document(
            "integration-test.txt",
            "text/plain",
            100,
            "test/integration-test.txt");

        var relevantEmbedding = new float[768];
        relevantEmbedding[0] = 1f;

        var irrelevantEmbedding = new float[768];
        irrelevantEmbedding[1] = 1f;

        document.AddChunk(
            "Germany industrial production is declining.",
            0,
            1,
            relevantEmbedding);

        document.AddChunk(
            "The weather is sunny today.",
            1,
            1,
            irrelevantEmbedding);

        db.Documents.Add(document);

        await db.SaveChangesAsync();

        var repository =
            new EFVectorSearchRepository(db);

        var queryEmbedding = new float[768];
        queryEmbedding[0] = 1f;

        var results =
            await repository.SearchAsync(
                queryEmbedding,
                topK: 5,
                minimumSimilarity: 0.80);

        Assert.Single(results);

        Assert.Equal(
            "Germany industrial production is declining.",
            results[0].Content);

        Assert.True(
            results[0].Similarity >= 0.99);
    }
}