using AegisRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisRAG.Infrastructure.Persistence;

public class AegisRagDbContext : DbContext
{
    public AegisRagDbContext(
        DbContextOptions<AegisRagDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AegisRagDbContext).Assembly);
    }
}