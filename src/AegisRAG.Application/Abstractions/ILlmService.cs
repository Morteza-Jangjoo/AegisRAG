namespace AegisRAG.Application.Abstractions;

public interface ILlmService
{
    Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> GenerateStreamingAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}