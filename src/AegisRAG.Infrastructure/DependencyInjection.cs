using AegisRAG.Application.Abstractions;
using AegisRAG.Infrastructure.Documents;
using AegisRAG.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AegisRAG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string fileStoragePath)
    {
        services.AddSingleton<IFileStorage>(
            new LocalFileStorage(fileStoragePath));

        services.AddSingleton<IDocumentRepository,
            InMemoryDocumentRepository>();

        return services;
    }
}