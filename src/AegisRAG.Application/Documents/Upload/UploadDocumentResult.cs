namespace AegisRAG.Application.Documents.Upload;

public sealed record UploadDocumentResult(
    Guid DocumentId,
    string FileName,
    string Status);