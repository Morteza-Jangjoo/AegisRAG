namespace AegisRAG.Application.Messaging;

public sealed record DocumentUploadedMessage(
    Guid DocumentId);