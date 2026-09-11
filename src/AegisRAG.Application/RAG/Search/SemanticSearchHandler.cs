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
    double minimumSimilarity = 0.40,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException(
                "Query is required.");

        if (topK <= 0)
            throw new ArgumentException(
                "TopK must be greater than zero.");

        if (topK > 10)
            throw new ArgumentException(
                "TopK cannot be greater than 10.");

        if (minimumSimilarity < 0 ||
            minimumSimilarity > 1)
        {
            throw new ArgumentException(
                "MinimumSimilarity must be between 0 and 1.");
        }

        var embedding =
            await _embeddingService.GenerateEmbeddingAsync(
                query,
                cancellationToken);

        var chunks =
            await _vectorSearchRepository.SearchAsync(
                embedding,
                topK,
                minimumSimilarity,
                cancellationToken);

        return new SemanticSearchResult(
            query,
            chunks);
    }
}