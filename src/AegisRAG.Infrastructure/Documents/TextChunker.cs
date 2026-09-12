using AegisRAG.Application.Abstractions;

namespace AegisRAG.Infrastructure.Documents;

public sealed class TextChunker : ITextChunker
{
    private readonly int _chunkSize;
    private readonly int _overlap;

    public TextChunker(
        int chunkSize = 1000,
        int overlap = 200)
    {
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize));

        if (overlap < 0 || overlap >= chunkSize)
            throw new ArgumentOutOfRangeException(nameof(overlap));

        _chunkSize = chunkSize;
        _overlap = overlap;
    }

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
                    _chunkSize,
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

                start += _chunkSize - _overlap;
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