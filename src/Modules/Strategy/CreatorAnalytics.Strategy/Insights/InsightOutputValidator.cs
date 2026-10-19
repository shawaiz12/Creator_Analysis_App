using CreatorAnalytics.Strategy.Domain;

namespace CreatorAnalytics.Strategy.Insights;

public static class InsightOutputValidator
{
    public static readonly string[] RequiredSections =
    {
        "What the data shows",
        "Script pacing fixes",
        "Thumbnail concepts"
    };

    /// <summary>
    /// Returns the cleaned draft, or throws a transient failure so the request is retried.
    /// Model output is never trusted as it arrives.
    /// </summary>
    public static string Validate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InsightGenerationException("The AI returned an empty draft.", isTransient: true);

        var text = new string(raw
            .Where(c => !char.IsControl(c) || c is '\n' or '\r' or '\t')
            .ToArray())
            .Trim();

        if (text.Length > StrategyDocument.MaxRevisionLength)
            throw new InsightGenerationException("The AI returned a draft that is too long.", isTransient: true);

        var missing = RequiredSections
            .Where(section => !text.Contains(section, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (missing.Count > 0)
        {
            throw new InsightGenerationException(
                $"The AI draft is missing these sections: {string.Join(", ", missing)}.", isTransient: true);
        }

        return text;
    }
}