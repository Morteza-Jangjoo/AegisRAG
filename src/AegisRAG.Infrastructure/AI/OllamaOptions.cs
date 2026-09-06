namespace AegisRAG.Infrastructure.AI;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } =
        "http://localhost:11434";

    public string EmbeddingModel { get; set; } =
        "nomic-embed-text";
}