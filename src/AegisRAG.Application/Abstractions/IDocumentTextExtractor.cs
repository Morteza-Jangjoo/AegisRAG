namespace AegisRAG.Application.Abstractions;

public interface IDocumentTextExtractor
{
    Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);
}

public sealed record ExtractedPage(
    int PageNumber,
    string Text);