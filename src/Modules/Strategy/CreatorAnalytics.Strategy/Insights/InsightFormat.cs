using System.Globalization;

namespace CreatorAnalytics.Strategy.Insights;

internal static class InsightFormat
{
    // Always the same format, whatever the computer's language settings are.
    public static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    public static string Clock(int seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);

        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : time.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }
}