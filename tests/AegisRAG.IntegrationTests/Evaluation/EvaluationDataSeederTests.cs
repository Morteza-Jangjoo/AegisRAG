using AegisRAG.Infrastructure.AI;
using AegisRAG.Infrastructure.Documents;
using AegisRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AegisRAG.IntegrationTests.Evaluation;

public class EvaluationDataSeederTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=aegisrag_test;Username=postgres;Password=postgres";

    [Fact]
    public async Task Seed_Evaluation_Document()
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

        await using var dbContext =
            new AegisRagDbContext(options);

        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE EXTENSION IF NOT EXISTS vector;");

        await dbContext.Database.MigrateAsync();

        var httpClient = new HttpClient
        {
            BaseAddress =
                new Uri("http://localhost:11434")
        };

        var ollamaOptions =
            Options.Create(
                new OllamaOptions
                {
                    BaseUrl = "http://localhost:11434",
                    EmbeddingModel = "nomic-embed-text"
                });

        var embeddingService =
            new OllamaEmbeddingService(
                httpClient,
                ollamaOptions);

        var textExtractor =
            new DocumentTextExtractor();

        var textChunker =
            new TextChunker();

        var seeder =
            new EvaluationDataSeeder(
                dbContext,
                textExtractor,
                embeddingService);

        var filePath =
            FindTestFile();

        await seeder.SeedAsync(
            filePath,
            1000,
            200);

        var document =
    await dbContext.Documents
        .Include(x => x.Chunks)
        .SingleAsync(
            x => x.FileName == "TestFile.txt");

        Assert.Equal(
            2,
            (int)document.Status);

        Assert.NotEmpty(document.Chunks);

        Assert.All(
            document.Chunks,
            chunk => Assert.NotNull(chunk.Embedding));
    }

    private static string FindTestFile()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate =
                Path.Combine(
                    directory.FullName,
                    "TestFile.txt");

            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Could not find TestFile.txt from project root.");
    }
}