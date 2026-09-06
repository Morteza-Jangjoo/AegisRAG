using AegisRAG.Domain.Entities;

namespace AegisRAG.Application.Abstractions;

public interface IDocumentRepository
{
    Task AddAsync(
        Document document,
        CancellationToken cancellationToken = default);

    Task<Document?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    
    Task UpdateAsync(
    Document document,
    CancellationToken cancellationToken = default);
}