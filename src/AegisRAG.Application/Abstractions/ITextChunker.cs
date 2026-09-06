namespace AegisRAG.Application.Abstractions;

public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(
        IReadOnlyList<ExtractedPage> pages);
}

public sealed record TextChunk(
    string Content,
    int ChunkIndex,
    int PageNumber);