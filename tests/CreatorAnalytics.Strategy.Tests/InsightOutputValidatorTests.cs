using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Insights;

namespace CreatorAnalytics.Strategy.Tests;

public class InsightOutputValidatorTests
{
    private const string Good =
        "## What the data shows\n- x\n## Script pacing fixes\n- y\n## Thumbnail concepts\n- z";

    [Fact]
    public void A_Good_Draft_Passes_And_Is_Trimmed()
    {
        var result = InsightOutputValidator.Validate("  " + Good + "\n");

        Assert.Equal(Good, result);
    }

    [Fact]
    public void An_Empty_Draft_Is_A_Transient_Failure()
    {
        var failure = Assert.Throws<InsightGenerationException>(() => InsightOutputValidator.Validate("   "));

        Assert.True(failure.IsTransient);
    }

    [Fact]
    public void A_Draft_That_Is_Too_Long_Is_A_Transient_Failure()
    {
        var tooLong = Good + new string('a', StrategyDocument.MaxRevisionLength);

        var failure = Assert.Throws<InsightGenerationException>(() => InsightOutputValidator.Validate(tooLong));

        Assert.True(failure.IsTransient);
    }

    [Fact]
    public void A_Missing_Section_Is_Named_In_The_Failure()
    {
        var withoutThumbnails = Good.Replace("Thumbnail concepts", "Something else");

        var failure = Assert.Throws<InsightGenerationException>(
            () => InsightOutputValidator.Validate(withoutThumbnails));

        Assert.True(failure.IsTransient);
        Assert.Contains("Thumbnail concepts", failure.Message);
    }

    [Fact]
    public void Control_Characters_Are_Removed_But_Line_Breaks_Stay()
    {
        var dirty = Good.Replace("x", "x\u0007\u001b");

        var result = InsightOutputValidator.Validate(dirty);

        Assert.DoesNotContain('\u0007', result);
        Assert.DoesNotContain('\u001b', result);
        Assert.Contains('\n', result);
    }

    [Fact]
    public async Task The_Template_Writers_Own_Output_Always_Passes()
    {
        var input = new InsightInput(
            "How I plan a month of videos",
            600,
            "Hello and welcome.",
            new[] { new DropOff(0, 60, 40.0, 60.0) },
            new[] { "Avoid 3D." });

        var result = await new TemplateInsightGenerator().GenerateAsync(input);

        InsightOutputValidator.Validate(result.Content);
    }
}