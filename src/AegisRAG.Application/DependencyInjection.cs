using AegisRAG.Application.Documents.Upload;
using Microsoft.Extensions.DependencyInjection;

namespace AegisRAG.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<UploadDocumentHandler>();

        return services;
    }
}