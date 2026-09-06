using AegisRAG.Application.Abstractions;
using UglyToad.PdfPig;

namespace AegisRAG.Infrastructure.Documents;

public sealed class DocumentTextExtractor
    : IDocumentTextExtractor
{
    public async Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        return contentType.ToLowerInvariant() switch
        {
            "text/plain" => await ExtractTextAsync(
                content,
                cancellationToken),

            "application/pdf" => ExtractPdf(
                content,
                cancellationToken),

            _ => throw new NotSupportedException(
                $"Content type '{contentType}' is not supported.")
        };
    }

    private static async Task<IReadOnlyList<ExtractedPage>> ExtractTextAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content);

        var text = await reader.ReadToEndAsync(
            cancellationToken);

        return
        [
            new ExtractedPage(
                1,
                text)
        ];
    }

    private static IReadOnlyList<ExtractedPage> ExtractPdf(
        Stream content,
        CancellationToken cancellationToken)
    {
        using var pdf = PdfDocument.Open(content);

        var pages = new List<ExtractedPage>();

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            pages.Add(
                new ExtractedPage(
                    page.Number,
                    page.Text));
        }

        return pages;
    }
}