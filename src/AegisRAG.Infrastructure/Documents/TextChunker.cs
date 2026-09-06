using AegisRAG.Application.Abstractions;

namespace AegisRAG.Infrastructure.Documents;

public sealed class TextChunker : ITextChunker
{
    private const int ChunkSize = 1000;
    private const int Overlap = 200;

    public IReadOnlyList<TextChunk> Chunk(
        IReadOnlyList<ExtractedPage> pages)
    {
        var chunks = new List<TextChunk>();

        var chunkIndex = 0;

        foreach (var page in pages)
        {
            var text = Normalize(page.Text);

            if (string.IsNullOrWhiteSpace(text))
                continue;

            var start = 0;

            while (start < text.Length)
            {
                var length = Math.Min(
                    ChunkSize,
                    text.Length - start);

                var chunkText = text.Substring(
                    start,
                    length);

                chunks.Add(
                    new TextChunk(
                        chunkText,
                        chunkIndex++,
                        page.PageNumber));

                if (start + length >= text.Length)
                    break;

                start += ChunkSize - Overlap;
            }
        }

        return chunks;
    }

    private static string Normalize(string text)
    {
        return string.Join(
            ' ',
            text.Split(
                [' ', '\r', '\n', '\t'],
                StringSplitOptions.RemoveEmptyEntries));
    }
}