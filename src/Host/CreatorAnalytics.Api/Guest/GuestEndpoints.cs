using CreatorAnalytics.Integration.Contracts.Services;
using CreatorAnalytics.Strategy.Insights;

namespace CreatorAnalytics.Api.Guest;

public sealed record GuestInsightRequest(
    string? VideoTitle,
    int DurationSeconds,
    string? Transcript,
    double[]? Retention,
    string[]? RejectionReasons);

public sealed record GuestInsightResponse(string Content, string Model, string PromptVersion);

public static class GuestEndpoints
{
    private const int MaxTitleCharacters = 200;
    private const int MaxTranscriptCharacters = 8_000;
    private const int MaxReasonCharacters = 500;
    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromSeconds(75);

    public static void MapGuestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost($"{GuestAccessExtensions.PathPrefix}/insights", CreateInsightAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> CreateInsightAsync(
        GuestInsightRequest request,
        IInsightGenerator generator,
        HttpContext http,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (!TryBuildInput(request, out var input, out var error))
            return Results.Json(new { error }, statusCode: StatusCodes.Status400BadRequest);

        logger.LogInformation(
    "Guest draft requested from {Ip} (connection {Remote}, forwarded-for {Forwarded}, cloudflare {Cf})",
    GuestAccessExtensions.ClientIp(http),
    http.Connection.RemoteIpAddress,
    http.Request.Headers["X-Forwarded-For"].ToString(),
    http.Request.Headers["CF-Connecting-IP"].ToString());

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(GenerationTimeout);

        try
        {
            var result = await generator.GenerateAsync(input, timeout.Token);
            return Results.Ok(new GuestInsightResponse(result.Content, result.Model, result.PromptVersion));
        }
        catch (InsightGenerationException ex)
        {
            logger.LogWarning(ex, "Guest draft failed (transient: {Transient})", ex.IsTransient);

            var message = ex.IsTransient
                ? "The AI is busy right now. Please try again in a minute."
                : "The demo AI is unavailable right now.";

            return Results.Json(new { error = message }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Results.Json(
                new { error = "The AI took too long to answer. Please try again." },
                statusCode: StatusCodes.Status504GatewayTimeout);
        }
    }

    // Everything a guest sends is untrusted, so every field is checked before it goes near the model.
    private static bool TryBuildInput(GuestInsightRequest request, out InsightInput input, out string error)
    {
        input = null!;

        var title = request.VideoTitle?.Trim();
        if (string.IsNullOrEmpty(title) || title.Length > MaxTitleCharacters)
        {
            error = $"The video title is required and limited to {MaxTitleCharacters} characters.";
            return false;
        }

        if (request.DurationSeconds is < 10 or > 21_600)
        {
            error = "The video length must be between 10 seconds and 6 hours.";
            return false;
        }

        var transcript = string.IsNullOrWhiteSpace(request.Transcript) ? null : request.Transcript.Trim();
        if (transcript is { Length: > MaxTranscriptCharacters })
        {
            error = $"In the demo, scripts are limited to {MaxTranscriptCharacters:N0} characters.";
            return false;
        }

        var retention = request.Retention;
        if (retention is null || retention.Length != 100 || retention.Any(v => !(v >= 0 && v <= 1)))
        {
            error = "Retention must be exactly 100 values between 0 and 1.";
            return false;
        }

        var points = retention
            .Select((value, index) => new RetentionSample((index + 1) / 100.0, value))
            .ToList();

        var dropOffs = RetentionAnalyzer.FindDropOffs(points, request.DurationSeconds);

        // Reasons an Admin gave for rejecting earlier drafts. Untrusted text, so it is bounded.
        var reasons = (request.RejectionReasons ?? Array.Empty<string>())
            .Select(reason => reason?.Trim() ?? string.Empty)
            .Where(reason => reason.Length > 0)
            .Select(reason => reason.Length > MaxReasonCharacters ? reason[..MaxReasonCharacters] : reason)
            .Take(InsightInputBuilder.MaxRejectionReasons)
            .ToList();

        input = new InsightInput(title, request.DurationSeconds, transcript, dropOffs, reasons);
        error = string.Empty;
        return true;
    }
}