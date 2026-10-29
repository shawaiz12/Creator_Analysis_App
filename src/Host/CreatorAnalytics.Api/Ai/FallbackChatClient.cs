using System.Net;
using Microsoft.Extensions.AI;

namespace CreatorAnalytics.Api.Ai;

/// <summary>
/// Tries a second model when the first one has a provider-side problem (server error, quota,
/// timeout, no answer). Configuration errors such as a bad key are never retried, because a
/// different model cannot fix them. Both inner clients must already map provider errors to
/// HttpRequestException (see StatusMappingChatClient).
/// </summary>
public sealed class FallbackChatClient : DelegatingChatClient
{
    private readonly IChatClient _fallback;
    private readonly ILogger<FallbackChatClient> _logger;

    public FallbackChatClient(IChatClient primary, IChatClient fallback, ILogger<FallbackChatClient> logger)
        : base(primary)
    {
        _fallback = fallback;
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
        catch (HttpRequestException ex) when (IsProviderSideProblem(ex))
        {
            _logger.LogWarning(ex,
                "The primary model failed (status: {Status}); trying the fallback model.",
                ex.StatusCode?.ToString() ?? "none");

            return await _fallback.GetResponseAsync(messages, options, cancellationToken);
        }
    }

    private static bool IsProviderSideProblem(HttpRequestException ex)
    {
        var status = ex.StatusCode;

        return status is null
            || status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            || (int)status.Value >= 500;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _fallback.Dispose();

        base.Dispose(disposing);
    }
}