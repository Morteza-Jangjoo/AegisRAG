using System.Diagnostics;

namespace AegisRAG.Application.Abstractions;

public static class AegisRagActivitySource
{
    public const string Name = "AegisRAG";

    public static readonly ActivitySource Source = new(Name);
}
