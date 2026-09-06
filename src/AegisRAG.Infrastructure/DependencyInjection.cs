using AegisRAG.Application.Abstractions;
using AegisRAG.Infrastructure.AI;
using AegisRAG.Infrastructure.Documents;
using AegisRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;

namespace AegisRAG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string fileStoragePath,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<AegisRagDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.UseVector();
                }));

        services.AddSingleton<IFileStorage>(
            new LocalFileStorage(fileStoragePath));

        services.AddScoped<IDocumentRepository,
            EFDocumentRepository>();

        services.AddScoped<IDocumentTextExtractor,
            DocumentTextExtractor>();

        services.AddSingleton<ITextChunker,
            TextChunker>();

        services.Configure<OllamaOptions>(
            configuration.GetSection(
                OllamaOptions.SectionName));

        services.AddHttpClient<IEmbeddingService,
            OllamaEmbeddingService>((serviceProvider, client) =>
            {
                var options =
                    serviceProvider
                        .GetRequiredService<
                            IOptions<OllamaOptions>>()
                        .Value;

                client.BaseAddress =
                    new Uri(options.BaseUrl);
            });

        services.AddScoped<IVectorSearchRepository,
            EFVectorSearchRepository>();

        return services;
    }
}