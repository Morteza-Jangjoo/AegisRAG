using AegisRAG.Application.Abstractions;
using AegisRAG.Infrastructure.Documents;
using AegisRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AegisRAG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string fileStoragePath,
        string connectionString)
    {
        services.AddDbContext<AegisRagDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<IFileStorage>(
            new LocalFileStorage(fileStoragePath));

        services.AddScoped<IDocumentRepository,
            EFDocumentRepository>();

        return services;
    }
}