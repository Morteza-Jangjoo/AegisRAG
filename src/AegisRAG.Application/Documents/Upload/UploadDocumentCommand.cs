namespace AegisRAG.Application.Documents.Upload;

public sealed record UploadDocumentCommand(
    string FileName,
    string ContentType,
    long FileSize,
    Stream Content);