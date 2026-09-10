using System.Runtime.CompilerServices;
using System.Text;
using AegisRAG.Application.Abstractions;
using AegisRAG.Application.RAG.Search;

namespace AegisRAG.Application.RAG.Chat;

public sealed class RagQueryHandler
{
    private readonly SemanticSearchHandler _semanticSearchHandler;
    private readonly ILlmService _llmService;

    public RagQueryHandler(
        SemanticSearchHandler semanticSearchHandler,
        ILlmService llmService)
    {
        _semanticSearchHandler = semanticSearchHandler;
        _llmService = llmService;
    }

    public async Task<RagQueryResult> HandleAsync(
        RagQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Question))
            throw new ArgumentException(
                "Question is required.");

        if (query.TopK <= 0)
            throw new ArgumentException(
                "TopK must be greater than zero.");

        var searchResult =
            await _semanticSearchHandler.HandleAsync(
                query.Question,
                query.TopK,
                cancellationToken);

        var sources = searchResult.Chunks
            .Select(x => new RagSourceDto(
                x.DocumentId,
                x.FileName,
                x.ChunkId,
                x.ChunkIndex,
                x.PageNumber,
                x.Similarity))
            .ToList();

        var prompt = BuildPrompt(
            query.Question,
            searchResult.Chunks);

        var answer =
            await _llmService.GenerateAsync(
                prompt,
                cancellationToken);

        return new RagQueryResult(
            query.Question,
            answer,
            sources);
    }

    private static string BuildPrompt(
    string question,
    IReadOnlyList<
        AegisRAG.Application.RAG.DTOs.SimilarChunkDto> chunks)
    {
        var context = new StringBuilder();

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];

            context.AppendLine(
                $"[Source {i + 1}]");

            context.AppendLine(
                $"File: {chunk.FileName}");

            if (chunk.PageNumber.HasValue)
            {
                context.AppendLine(
                    $"Page: {chunk.PageNumber}");
            }

            context.AppendLine(
                $"Similarity: {chunk.Similarity:F3}");

            context.AppendLine(
                "Content:");

            context.AppendLine(chunk.Content);

            context.AppendLine();
        }

        return $"""
        You are a helpful AI assistant.

        Answer the user's question using only the
        information provided in the sources below.

        Important rules:
        - Do not use outside knowledge.
        - If the answer cannot be found in the sources,
          say that you don't have enough information.
        - When making a factual claim, cite the relevant
          source using [Source N].
        - Do not invent source numbers.

        Sources:
        {context}

        Question:
        {question}

        Answer:
        """;
    }


    public async IAsyncEnumerable<string> StreamAsync(
    RagQuery query,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[RAG] StreamAsync started.");

        var searchResult =
            await _semanticSearchHandler.HandleAsync(
                query.Question,
                query.TopK,
                cancellationToken);


        var prompt = BuildPrompt(
            query.Question,
            searchResult.Chunks);

        Console.WriteLine("[RAG] Before LLM streaming.");

        await foreach (var chunk in
            _llmService.GenerateStreamingAsync(
                prompt,
                cancellationToken))
        {
            Console.WriteLine(
        $"[RAG] Yielding chunk: '{chunk}'");

            yield return chunk;

            Console.WriteLine(
        $"[RAG] Chunk returned to caller: '{chunk}'");
        }

        Console.WriteLine("[RAG] StreamAsync completed.");
    }
}