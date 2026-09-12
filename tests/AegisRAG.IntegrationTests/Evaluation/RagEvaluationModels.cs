namespace AegisRAG.IntegrationTests.Evaluation;

public sealed class EvaluationQuestion
{
    public string Question { get; set; } = null!;

    public bool Relevant { get; set; }

    public List<ExpectedSource> ExpectedSources { get; set; } = [];
}

public sealed class ExpectedSource
{
    public string FileName { get; set; } = null!;

    public int PageNumber { get; set; }

    public List<string> ExpectedTerms { get; set; } = [];
}

public sealed record EvaluationResult(
    double Threshold,
    int TotalQuestions,
    int RelevantQuestions,
    int IrrelevantQuestions,
    int HitAtK,
    double HitRate,
    double Precision,
    double Recall,
    double F1,
    double FalsePositiveRate);