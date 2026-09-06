using System.Collections.Concurrent;
using AegisRAG.Application.Abstractions;
using AegisRAG.Domain.Entities;

namespace AegisRAG.Infrastructure.Persistence;

public sealed class InMemoryDocumentRepository
    : IDocumentRepository
{
    private readonly ConcurrentDictionary<Guid, Document> _documents = [];

    public Task AddAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        _documents[document.Id] = document;

        return Task.CompletedTask;
    }

    public Task<Document?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _documents.TryGetValue(
            id,
            out var document);

        return Task.FromResult(document);
    }
}