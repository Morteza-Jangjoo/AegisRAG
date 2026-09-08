using AegisRAG.Application;
using AegisRAG.Infrastructure;
using AegisRAG.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

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