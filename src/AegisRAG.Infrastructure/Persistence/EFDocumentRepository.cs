using AegisRAG.Application.Abstractions;
using AegisRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisRAG.Infrastructure.Persistence;

public sealed class EFDocumentRepository
    : IDocumentRepository
{
    private readonly AegisRagDbContext _dbContext;

    public EFDocumentRepository(
        AegisRagDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Documents.AddAsync(
            document,
            cancellationToken);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<Document?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .Include(x => x.Chunks)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }
}