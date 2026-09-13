using System.Text.Json;
using AegisRAG.Application.Abstractions;
using AegisRAG.Infrastructure.AI;
using AegisRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AegisRAG.IntegrationTests.Evaluation;

public class RagEvaluationRunner
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=aegisrag_test;Username=postgres;Password=postgres";

    private const int TopK = 5;

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
    public async Task Run_Rag_Evaluation()
    {
        var questions = await LoadQuestionsAsync();

        Assert.NotEmpty(questions);

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
                    EmbeddingModel = "nomic-embed-text",
                });

        var embeddingService =
            new OllamaEmbeddingService(
                httpClient,
                ollamaOptions);

        var repository =
            new EFVectorSearchRepository(db);

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("AegisRAG RAG Evaluation");
        Console.WriteLine("========================================");
        Console.WriteLine(
            $"Questions : {questions.Count}");
        Console.WriteLine(
            $"TopK      : {TopK}");
        Console.WriteLine(
            $"Thresholds: {string.Join(", ", Thresholds)}");
        Console.WriteLine();

        var queryResults =
            new List<QueryEvaluation>();

        foreach (var question in questions)
        {
            Console.WriteLine(
                $"Embedding: {question.Question}");

            var embedding =
                await embeddingService.GenerateEmbeddingAsync(
                    question.Question);

            var results =
                await repository.SearchAsync(
                    embedding,
                    TopK,
                    minimumSimilarity: 0);

            queryResults.Add(
                new QueryEvaluation(
                    question,
                    results));
        }

        Console.WriteLine();
        Console.WriteLine("Evaluation results");
        Console.WriteLine();

        var evaluationResults =
            new List<EvaluationResult>();

        foreach (var threshold in Thresholds)
        {
            var result =
                Evaluate(
                    queryResults,
                    threshold);

            evaluationResults.Add(result);

            Console.WriteLine(
                $"Threshold: {result.Threshold:F2}");

            Console.WriteLine(
                $"  Hit@{TopK}:          {result.HitRate:P1}");

            Console.WriteLine(
                $"  Precision@{TopK}:    {result.Precision:P1}");

            Console.WriteLine(
                $"  Recall@{TopK}:       {result.Recall:P1}");

            Console.WriteLine(
                $"  F1:                  {result.F1:P1}");

            Console.WriteLine(
                $"  False Positive Rate: {result.FalsePositiveRate:P1}");

            Console.WriteLine();
        }

        PrintBestThreshold(evaluationResults);

        Console.WriteLine();
        //PrintQuestionAnalysis(queryResults, 0.65);
    }

    private static async Task<List<EvaluationQuestion>>
        LoadQuestionsAsync()
    {
        var path = Path.Combine(
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

            if (retrieved.Any(
                    x => IsRelevant(
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
                retrieved.Count(
                    x => IsRelevant(
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

    private static void PrintBestThreshold(
        IReadOnlyList<EvaluationResult> results)
    {
        var best =
            results
                .OrderByDescending(x => x.F1)
                .ThenBy(x => x.FalsePositiveRate)
                .First();

        Console.WriteLine(
            "========================================");

        Console.WriteLine(
            $"Recommended threshold: {best.Threshold:F2}");

        Console.WriteLine(
            $"F1: {best.F1:P1}");

        Console.WriteLine(
            $"Precision: {best.Precision:P1}");

        Console.WriteLine(
            $"Recall: {best.Recall:P1}");

        Console.WriteLine(
            $"False Positive Rate: {best.FalsePositiveRate:P1}");

        Console.WriteLine(
            "========================================");
    }

    private static void PrintQuestionAnalysis(
    IReadOnlyList<QueryEvaluation> queryResults,
    double threshold)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(
            $"Per-Question Analysis (Threshold: {threshold:F2})");
        Console.WriteLine("========================================");

        foreach (var query in queryResults)
        {
            var retrieved =
                query.Results
                    .Where(x => x.Similarity >= threshold)
                    .ToList();

            var hasRelevantChunk =
                retrieved.Any(x =>
                    IsRelevant(x, query.Question));

            var expected =
                query.Question.Relevant
                    ? "RELEVANT"
                    : "IRRELEVANT";

            var result =
                query.Question.Relevant
                    ? hasRelevantChunk ? "PASS" : "FAIL"
                    : retrieved.Count == 0 ? "PASS" : "FALSE POSITIVE";

            Console.WriteLine();
            Console.WriteLine(
                $"Question: {query.Question.Question}");
            Console.WriteLine(
                $"Expected: {expected}");
            Console.WriteLine(
                $"Result:   {result}");

            Console.WriteLine("Top results:");

            foreach (var chunk in query.Results)
            {
                var relevant =
                    IsRelevant(chunk, query.Question);

                Console.WriteLine(
                    $"  Similarity={chunk.Similarity:F3} " +
                    $"Chunk={chunk.ChunkIndex} " +
                    $"Page={chunk.PageNumber} " +
                    $"Relevant={relevant}");

                if (relevant)
                {
                    Console.WriteLine(
                        $"    Content: {chunk.Content}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
    }

    private sealed record QueryEvaluation(
        EvaluationQuestion Question,
        IReadOnlyList<
            AegisRAG.Application.RAG.DTOs.SimilarChunkDto>
            Results);
}