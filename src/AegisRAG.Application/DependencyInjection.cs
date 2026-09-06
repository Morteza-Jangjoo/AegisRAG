using AegisRAG.Application.Documents.Process;
using AegisRAG.Application.Documents.Upload;
using AegisRAG.Application.RAG.Search;
using Microsoft.Extensions.DependencyInjection;

namespace AegisRAG.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<UploadDocumentHandler>();

        services.AddScoped<ProcessDocumentHandler>();

        services.AddScoped<SemanticSearchHandler>();

        return services;
    }
}