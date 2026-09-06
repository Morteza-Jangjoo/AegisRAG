using AegisRAG.Application.Abstractions;

namespace AegisRAG.Application.RAG.Search;

public sealed class SemanticSearchHandler
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorSearchRepository _vectorSearchRepository;

    public SemanticSearchHandler(
        IEmbeddingService embeddingService,
        IVectorSearchRepository vectorSearchRepository)
    {
        _embeddingService = embeddingService;
        _vectorSearchRepository = vectorSearchRepository;
    }

    public async Task<SemanticSearchResult> HandleAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException(
                "Query is required.");

        var embedding =
            await _embeddingService.GenerateEmbeddingAsync(
                query,
                cancellationToken);

        var chunks =
            await _vectorSearchRepository.SearchAsync(
                embedding,
                topK,
                cancellationToken);

        return new SemanticSearchResult(
            query,
            chunks);
    }
}