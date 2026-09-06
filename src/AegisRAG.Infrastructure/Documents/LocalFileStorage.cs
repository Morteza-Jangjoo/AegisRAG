using AegisRAG.Application.Abstractions;

namespace AegisRAG.Infrastructure.Documents;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(string rootPath)
    {
        _rootPath = rootPath;

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var fileId = Guid.NewGuid();

        var extension = Path.GetExtension(fileName);

        var storedFileName = $"{fileId}{extension}";

        var fullPath = Path.Combine(
            _rootPath,
            storedFileName);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await content.CopyToAsync(
            fileStream,
            cancellationToken);

        return fullPath;
    }

    public Task DeleteAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}