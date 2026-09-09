namespace AegisRAG.Application.RAG.Chat;

public sealed record RagQueryResult(
    string Question,
    string Answer,
    IReadOnlyList<RagSourceDto> Sources);