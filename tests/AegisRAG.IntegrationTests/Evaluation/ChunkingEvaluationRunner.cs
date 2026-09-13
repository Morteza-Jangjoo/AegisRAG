using System.Text.Json;
using AegisRAG.Application.Abstractions;
using AegisRAG.Infrastructure.AI;
using AegisRAG.Infrastructure.Documents;
using AegisRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AegisRAG.IntegrationTests.Evaluation;

public class ChunkingEvaluationRunner
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=aegisrag_test;Username=postgres;Password=postgres";

    private const int TopK = 5;

    // private static readonly ChunkingConfiguration[] Configurations =
    // [
    //     new("1000/200", 1000, 200),
    //     new("700/150", 700, 150),
    //     new("500/100", 500, 100)
    // ];

    // private static readonly double[] Thresholds =
    // [
    //     0.55,
    //     0.56,
    //     0.57,
    //     0.58,
    //     0.59,
    //     0.60,
    //     0.61,
    //     0.62,
    //     0.63,
    //     0.64,
    //     0.65
    // ];
    private static readonly ChunkingConfiguration[] Configurations =
[
    new("700/150", 700, 150)
];

    private static readonly double[] Thresholds =
[
    0.66,
    0.67,
    0.68,
    0.69,
    0.70,
    0.71,
    0.72
];

    [Fact]
    public async Task Run_Chunking_Evaluation()
    {
        var questions =
            await LoadQuestionsAsync();

        Assert.NotEmpty(questions);

        var filePath =
            FindEvaluationFile();

        var results =
            new List<ChunkingEvaluationResult>();

        foreach (var configuration in Configurations)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine(
                $"Chunking: {configuration.Name}");
            Console.WriteLine("========================================");

            await SeedAsync(
                filePath,
                configuration);

            var queryResults =
                await RunQueriesAsync(questions);

            var best =
                FindBestResult(
                    queryResults);

            results.Add(
                new ChunkingEvaluationResult(
                    configuration,
                    best));

            PrintResult(
                configuration,
                best);
        }

        PrintComparison(results);
    }

    private async Task SeedAsync(
        string filePath,
        ChunkingConfiguration configuration)
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

        var textExtractor =
            new DocumentTextExtractor();

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

        var seeder =
            new EvaluationDataSeeder(
                dbContext,
                textExtractor,
                embeddingService);

        Console.WriteLine(
            $"Seeding {configuration.ChunkSize}/{configuration.Overlap}...");

        await seeder.SeedAsync(
            filePath,
            configuration.ChunkSize,
            configuration.Overlap);

        Console.WriteLine("Seeding completed.");
    }

    private async Task<List<QueryEvaluation>> RunQueriesAsync(
        IReadOnlyList<EvaluationQuestion> questions)
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

        var repository =
            new EFVectorSearchRepository(db);

        var queryResults =
            new List<QueryEvaluation>();

        foreach (var question in questions)
        {
            Console.WriteLine(
                $"Embedding: {question.Question}");

            var embedding =
                await embeddingService.GenerateEmbeddingAsync(
                    question.Question);

            var searchResults =
                await repository.SearchAsync(
                    embedding,
                    TopK,
                    minimumSimilarity: 0);

            queryResults.Add(
                new QueryEvaluation(
                    question,
                    searchResults));
        }

        return queryResults;
    }

    private static EvaluationResult FindBestResult(
        IReadOnlyList<QueryEvaluation> queryResults)
    {
        var results =
            Thresholds
                .Select(threshold =>
                    Evaluate(
                        queryResults,
                        threshold))
                .ToList();

        return results
            .OrderByDescending(x => x.F1)
            .ThenBy(x => x.FalsePositiveRate)
            .First();
    }

    private static EvaluationResult Evaluate(
        IReadOnlyList<QueryEvaluation> queryResults,
        double threshold)
    {
        var relevantQuestions =
            queryResults
                .Where(x => x.Question.Relevant)
                .ToList();

        var irrelevantQuestions =
            queryResults
                .Where(x => !x.Question.Relevant)
                .ToList();

        var hits = 0;

        foreach (var query in relevantQuestions)
        {
            var retrieved =
                query.Results
                    .Where(x =>
                        x.Similarity >= threshold)
                    .ToList();

            if (retrieved.Any(x =>
                    IsRelevant(
                        x,
                        query.Question)))
            {
                hits++;
            }
        }

        var relevantRetrieved = 0;
        var totalRetrieved = 0;

        foreach (var query in relevantQuestions)
        {
            var retrieved =
                query.Results
                    .Where(x =>
                        x.Similarity >= threshold)
                    .ToList();

            totalRetrieved += retrieved.Count;

            relevantRetrieved +=
                retrieved.Count(x =>
                    IsRelevant(
                        x,
                        query.Question));
        }

        var falsePositiveQueries = 0;

        foreach (var query in irrelevantQuestions)
        {
            var retrieved =
                query.Results
                    .Where(x =>
                        x.Similarity >= threshold)
                    .ToList();

            if (retrieved.Count > 0)
                falsePositiveQueries++;
        }

        var hitRate =
            relevantQuestions.Count == 0
                ? 0
                : (double)hits /
                  relevantQuestions.Count;

        var precision =
            totalRetrieved == 0
                ? 0
                : (double)relevantRetrieved /
                  totalRetrieved;

        var recall = hitRate;

        var f1 =
            precision + recall == 0
                ? 0
                : 2 *
                  precision *
                  recall /
                  (precision + recall);

        var falsePositiveRate =
            irrelevantQuestions.Count == 0
                ? 0
                : (double)falsePositiveQueries /
                  irrelevantQuestions.Count;

        return new EvaluationResult(
            threshold,
            queryResults.Count,
            relevantQuestions.Count,
            irrelevantQuestions.Count,
            hits,
            hitRate,
            precision,
            recall,
            f1,
            falsePositiveRate);
    }

    private static bool IsRelevant(
        AegisRAG.Application.RAG.DTOs.SimilarChunkDto chunk,
        EvaluationQuestion question)
    {
        foreach (var source in question.ExpectedSources)
        {
            if (!string.Equals(
                    chunk.FileName,
                    source.FileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (chunk.PageNumber != source.PageNumber)
                continue;

            if (source.ExpectedTerms.Count == 0)
                return true;

            var matchingTerms =
                source.ExpectedTerms.Count(term =>
                    chunk.Content.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase));

            return matchingTerms >=
                   Math.Max(
                       1,
                       (int)Math.Ceiling(
                           source.ExpectedTerms.Count * 0.5));
        }

        return false;
    }

    private static void PrintResult(
        ChunkingConfiguration configuration,
        EvaluationResult result)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"Best result for {configuration.Name}");

        Console.WriteLine(
            $"  Threshold:          {result.Threshold:F2}");

        Console.WriteLine(
            $"  Hit@{TopK}:             {result.HitRate:P1}");

        Console.WriteLine(
            $"  Precision@{TopK}:       {result.Precision:P1}");

        Console.WriteLine(
            $"  Recall@{TopK}:          {result.Recall:P1}");

        Console.WriteLine(
            $"  F1:                   {result.F1:P1}");

        Console.WriteLine(
            $"  False Positive Rate:  {result.FalsePositiveRate:P1}");
    }

    private static void PrintComparison(
        IReadOnlyList<ChunkingEvaluationResult> results)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("CHUNKING EVALUATION COMPARISON");
        Console.WriteLine("========================================");

        Console.WriteLine(
            $"{"Configuration",-18}" +
            $"{"Threshold",-12}" +
            $"{"Precision",-12}" +
            $"{"Recall",-12}" +
            $"{"F1",-12}" +
            $"{"FPR",-12}");

        Console.WriteLine(
            new string('-', 78));

        foreach (var result in results)
        {
            var evaluation =
                result.BestResult;

            Console.WriteLine(
                $"{result.Configuration.Name,-18}" +
                $"{evaluation.Threshold,-12:F2}" +
                $"{evaluation.Precision,-12:P1}" +
                $"{evaluation.Recall,-12:P1}" +
                $"{evaluation.F1,-12:P1}" +
                $"{evaluation.FalsePositiveRate,-12:P1}");
        }

        var best =
            results
                .OrderByDescending(x =>
                    x.BestResult.F1)
                .ThenBy(x =>
                    x.BestResult.FalsePositiveRate)
                .First();

        Console.WriteLine();
        Console.WriteLine(
            $"Recommended chunking: " +
            $"{best.Configuration.ChunkSize}/{best.Configuration.Overlap}");

        Console.WriteLine(
            $"Recommended threshold: " +
            $"{best.BestResult.Threshold:F2}");

        Console.WriteLine(
            $"F1: {best.BestResult.F1:P1}");

        Console.WriteLine(
            $"Precision: {best.BestResult.Precision:P1}");

        Console.WriteLine(
            $"Recall: {best.BestResult.Recall:P1}");

        Console.WriteLine(
            $"FPR: {best.BestResult.FalsePositiveRate:P1}");

        Console.WriteLine(
            "========================================");
    }

    private static async Task<List<EvaluationQuestion>>
        LoadQuestionsAsync()
    {
        var path =
            Path.Combine(
                AppContext.BaseDirectory,
                "Evaluation",
                "rag-evaluation.json");

        await using var stream =
            File.OpenRead(path);

        var questions =
            await JsonSerializer.DeserializeAsync<
                List<EvaluationQuestion>>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return questions ?? [];
    }

    private static string FindEvaluationFile()
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

    private sealed record ChunkingConfiguration(
        string Name,
        int ChunkSize,
        int Overlap);

    private sealed record QueryEvaluation(
        EvaluationQuestion Question,
        IReadOnlyList<
            AegisRAG.Application.RAG.DTOs.SimilarChunkDto>
            Results);

    private sealed record ChunkingEvaluationResult(
        ChunkingConfiguration Configuration,
        EvaluationResult BestResult);

    private sealed record EvaluationResult(
        double Threshold,
        int TotalQuestions,
        int RelevantQuestions,
        int IrrelevantQuestions,
        int Hits,
        double HitRate,
        double Precision,
        double Recall,
        double F1,
        double FalsePositiveRate);
}