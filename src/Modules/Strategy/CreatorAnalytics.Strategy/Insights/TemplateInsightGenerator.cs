using System.Globalization;
using System.Text;

namespace CreatorAnalytics.Strategy.Insights;

/// <summary>
/// A deterministic draft writer: the same facts always produce the same text.
/// The real AI generator will be added behind the same interface.
/// </summary>
public sealed class TemplateInsightGenerator : IInsightGenerator
{
    public const string ModelName = "template";
    public const string Version = "template-v1";

    private const double EarlyShare = 0.15;
    private const int SnippetLength = 160;

    public Task<InsightResult> GenerateAsync(
        InsightInput input, CancellationToken cancellationToken = default)
    {
        var text = new StringBuilder();

        text.AppendLine($"# Draft strategy for \"{input.VideoTitle}\"");
        text.AppendLine();

        text.AppendLine("## What the data shows");

        if (input.DropOffs.Count == 0)
        {
            text.AppendLine("- No section of this video loses a notable share of viewers. Keep the current structure.");
        }
        else
        {
            foreach (var drop in input.DropOffs)
            {
                text.AppendLine(
                    $"- {Clock(drop.StartSecond)}-{Clock(drop.EndSecond)}: {Number(drop.PointsLost)} points of viewers leave; " +
                    $"{Number(drop.RetainedPercent)}% are still watching at {Clock(drop.EndSecond)}.");
            }
        }

        text.AppendLine();
        text.AppendLine("## Script pacing fixes");

        if (input.DropOffs.Count == 0)
            text.AppendLine("- Nothing to fix in the pacing. Consider testing a stronger opening line anyway.");

        foreach (var drop in input.DropOffs)
        {
            var early = input.DurationSeconds > 0 && drop.StartSecond < EarlyShare * input.DurationSeconds;

            text.AppendLine(early
                ? $"- Around {Clock(drop.StartSecond)}: open with the payoff in the first 15 seconds, then explain how you got there. Cut any intro or branding before the first useful sentence."
                : $"- Just before {Clock(drop.StartSecond)}: add a pattern interrupt, such as a new visual, a question to the viewer, or a one-line recap of what comes next.");

            var snippet = Snippet(input.Transcript, input.DurationSeconds, drop.StartSecond);

            if (snippet is not null)
                text.AppendLine($"  What the script says around {Clock(drop.StartSecond)}: \"{snippet}\"");
        }

        if (string.IsNullOrWhiteSpace(input.Transcript))
            text.AppendLine("- No transcript has been added for this video, so these suggestions use the retention data only.");

        text.AppendLine();
        text.AppendLine("## Thumbnail concepts");
        text.AppendLine($"- Concept A: a close-up of your face reacting, with the words \"{Promise(input.VideoTitle)}\" in large text.");
        text.AppendLine("- Concept B: a before/after split that shows the result of the video.");
        text.AppendLine("- Concept C: one bold object from the video with a single-word label.");

        if (input.RejectionReasons.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("## Constraints from earlier reviews");

            foreach (var reason in input.RejectionReasons)
                text.AppendLine($"- {reason}");

            text.AppendLine("Check every suggestion above against these before submitting.");
        }

        return Task.FromResult(new InsightResult(text.ToString().TrimEnd(), ModelName, Version));
    }

    // Always the same format, whatever the computer's language settings are.
    private static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static string Clock(int seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);

        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : time.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    private static string Promise(string title) =>
        string.Join(' ', title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(4));

    private static string? Snippet(string? transcript, int durationSeconds, int atSecond)
    {
        if (string.IsNullOrWhiteSpace(transcript) || durationSeconds <= 0)
            return null;

        var clean = string.Join(' ', transcript.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var position = (int)((double)atSecond / durationSeconds * clean.Length);
        var start = Math.Clamp(position, 0, Math.Max(0, clean.Length - SnippetLength));
        var length = Math.Min(SnippetLength, clean.Length - start);

        // The transcript is untrusted text: it is only ever quoted, never interpreted.
        return clean.Substring(start, length).Replace("\"", "'");
    }
}