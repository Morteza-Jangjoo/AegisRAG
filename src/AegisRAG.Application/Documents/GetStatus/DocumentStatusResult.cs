using AegisRAG.Domain.Enums;

namespace AegisRAG.Application.Documents.GetStatus;

public sealed record DocumentStatusResult(
    Guid Id,
    string FileName,
    DocumentStatus Status,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    int ChunkCount);