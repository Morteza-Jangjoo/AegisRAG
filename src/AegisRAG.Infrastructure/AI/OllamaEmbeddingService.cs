using System.Net.Http.Json;
using AegisRAG.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AegisRAG.Infrastructure.AI;

public sealed class OllamaEmbeddingService
    : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaEmbeddingService(
        HttpClient httpClient,
        IOptions<OllamaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _options.EmbeddingModel,
            input = text
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/embed",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(
                cancellationToken);

        if (result?.Embeddings is null ||
            result.Embeddings.Length == 0)
        {
            throw new InvalidOperationException(
                "Ollama returned an empty embedding.");
        }

        return result.Embeddings[0];
    }

    private sealed record OllamaEmbeddingResponse(
        float[][] Embeddings);
}