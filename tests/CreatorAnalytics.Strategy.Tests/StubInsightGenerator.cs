using CreatorAnalytics.Strategy.Insights;

namespace CreatorAnalytics.Strategy.Tests;

/// <summary>A generator whose failures the test controls; otherwise it writes the template draft.</summary>
public sealed class StubInsightGenerator : IInsightGenerator
{
    private readonly TemplateInsightGenerator _inner = new();

    public Exception? ThrowOnGenerate { get; set; }

    public InsightInput? LastInput { get; private set; }

    public int Calls { get; private set; }

    public Task<InsightResult> GenerateAsync(InsightInput input, CancellationToken cancellationToken = default)
    {
        Calls++;
        LastInput = input;

        if (ThrowOnGenerate is not null)
            throw ThrowOnGenerate;

        return _inner.GenerateAsync(input, cancellationToken);
    }
}