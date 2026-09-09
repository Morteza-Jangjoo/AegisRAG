using System.Net.Http.Json;
using AegisRAG.Application.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisRAG.Infrastructure.AI;

public sealed class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaLlmService(
        HttpClient httpClient,
        IOptions<OllamaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _options.LlmModel,
            prompt,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/generate",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
                cancellationToken);

        if (string.IsNullOrWhiteSpace(result?.Response))
        {
            throw new InvalidOperationException(
                "Ollama returned an empty response.");
        }

        return result.Response;
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(
    string prompt,
    [System.Runtime.CompilerServices.EnumeratorCancellation]
    CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _options.LlmModel,
            prompt,
            stream = true
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/generate")
        {
            Content = JsonContent.Create(request)
        };

        Console.WriteLine("[Ollama] Streaming request started.");

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        Console.WriteLine(
            $"[Ollama] Response status: {response.StatusCode}");

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        Console.WriteLine("[Ollama] Response stream opened.");

        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(
                cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            Console.WriteLine(
        $"[Ollama] Raw chunk: {line}");

            var result =
                JsonSerializer.Deserialize<OllamaStreamResponse>(
                    line);

            Console.WriteLine(
    $"[Ollama] Parsed: Response='{result?.Response}', Done={result?.Done}");

            if (result is null)
                continue;

            if (!string.IsNullOrEmpty(result.Response))
            {
                Console.WriteLine(
                    $"[Ollama] Yielding response: '{result.Response}'");

                yield return result.Response;

                Console.WriteLine(
                    $"[Ollama] Response returned to caller: '{result.Response}'");
            }

            if (result.Done)
            {
                Console.WriteLine("[Ollama] Stream completed.");
                yield break;
            }
        }
    }

    private sealed record OllamaStreamResponse(
    [property: JsonPropertyName("response")] string? Response,
    [property: JsonPropertyName("done")] bool Done);

    private sealed record OllamaGenerateResponse(
        string Response);
}