namespace AegisRAG.Application.RAG.Chat;

public sealed record RagQuery(
    string Question,
    int TopK = 5);