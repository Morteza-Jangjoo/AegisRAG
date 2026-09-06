using AegisRAG.Application.RAG.DTOs;

namespace AegisRAG.Application.RAG.Search;

public sealed record SemanticSearchResult(
    string Query,
    IReadOnlyList<SimilarChunkDto> Chunks);