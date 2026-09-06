namespace AegisRAG.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string path,
        CancellationToken cancellationToken = default);
}