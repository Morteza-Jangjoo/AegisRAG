using AegisRAG.Application;
using AegisRAG.Infrastructure;
using AegisRAG.Worker.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: builder.Environment.ApplicationName))
    .WithTracing(tracing =>
    {
        tracing.AddHttpClientInstrumentation();
        tracing.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(
                !string.IsNullOrWhiteSpace(otlpEndpoint)
                    ? otlpEndpoint
                    : "http://localhost:4317");
        });
    });

builder.Services.AddApplication();

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string is missing.");

builder.Services.AddInfrastructure(
    Path.Combine(
        builder.Environment.ContentRootPath,
        "storage"),
    connectionString,
    builder.Configuration);

builder.Services.AddHostedService<DocumentProcessingWorker>();

var host = builder.Build();

host.Run();