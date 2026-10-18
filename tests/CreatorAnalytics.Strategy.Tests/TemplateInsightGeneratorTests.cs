using System.Globalization;
using CreatorAnalytics.Strategy.Insights;

namespace CreatorAnalytics.Strategy.Tests;

public class TemplateInsightGeneratorTests
{
    private readonly TemplateInsightGenerator _generator = new();

    private static InsightInput Input(
        IReadOnlyList<DropOff>? drops = null,
        string? transcript = null,
        IReadOnlyList<string>? reasons = null) =>
        new("How I plan a month of videos",
            600,
            transcript,
            drops ?? new[] { new DropOff(0, 60, 40.0, 60.0) },
            reasons ?? Array.Empty<string>());

    [Fact]
    public async Task The_Draft_Names_The_Video_And_The_Biggest_Drop_And_Labels_Itself()
    {
        var result = await _generator.GenerateAsync(Input());

        Assert.Contains("How I plan a month of videos", result.Content);
        Assert.Contains("0:00-1:00", result.Content);
        Assert.Contains("40 points of viewers leave", result.Content);
        Assert.Contains("60% are still watching at 1:00", result.Content);
        Assert.Equal("template", result.Model);
        Assert.Equal("template-v1", result.PromptVersion);
    }

    [Fact]
    public async Task An_Early_Drop_Gets_Hook_Advice()
    {
        var result = await _generator.GenerateAsync(Input());

        Assert.Contains("open with the payoff", result.Content);
    }

    [Fact]
    public async Task A_Later_Drop_Gets_Pattern_Interrupt_Advice()
    {
        var result = await _generator.GenerateAsync(Input(new[] { new DropOff(300, 360, 6.0, 40.0) }));

        Assert.Contains("pattern interrupt", result.Content);
        Assert.Contains("5:00", result.Content);
    }

    [Fact]
    public async Task The_Script_Near_The_Drop_Is_Quoted_When_A_Transcript_Exists()
    {
        var transcript = string.Concat(Enumerable.Repeat("Hello and welcome. ", 50));

        var result = await _generator.GenerateAsync(Input(transcript: transcript));

        Assert.Contains("What the script says around 0:00: \"Hello and welcome.", result.Content);
    }

    [Fact]
    public async Task A_Missing_Transcript_Is_Said_Plainly()
    {
        var result = await _generator.GenerateAsync(Input());

        Assert.Contains("No transcript has been added", result.Content);
    }

    [Fact]
    public async Task Earlier_Rejection_Reasons_Appear_As_Constraints()
    {
        var result = await _generator.GenerateAsync(
            Input(reasons: new[] { "Avoid anything that needs 3D animation." }));

        Assert.Contains("## Constraints from earlier reviews", result.Content);
        Assert.Contains("Avoid anything that needs 3D animation.", result.Content);
    }

    [Fact]
    public async Task The_Same_Facts_Always_Produce_The_Same_Draft()
    {
        var first = await _generator.GenerateAsync(Input(transcript: "Hello and welcome."));
        var second = await _generator.GenerateAsync(Input(transcript: "Hello and welcome."));

        Assert.Equal(first.Content, second.Content);
    }

    [Fact]
    public async Task Numbers_Are_Formatted_The_Same_In_Every_Culture()
    {
        var original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            var result = await _generator.GenerateAsync(Input(new[] { new DropOff(120, 180, 4.4, 55.6) }));

            Assert.Contains("4.4 points", result.Content);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}