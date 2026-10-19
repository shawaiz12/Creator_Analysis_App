using System.Net;
using Microsoft.Extensions.AI;

namespace CreatorAnalytics.Strategy.Insights;

/// <summary>
/// Writes drafts with a language model reached through the provider-neutral IChatClient.
/// The model only ever sees what the prompt builder puts in front of it.
/// </summary>
public sealed class ChatInsightGenerator : IInsightGenerator
{
    private const int MaxModelLabelLength = 100;

    private readonly IChatClient _chat;

    public ChatInsightGenerator(IChatClient chat)
    {
        _chat = chat;
    }

    public async Task<InsightResult> GenerateAsync(
        InsightInput input, CancellationToken cancellationToken = default)
    {
        var prompt = InsightPromptBuilder.Build(input);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, prompt.System),
            new(ChatRole.User, prompt.User)
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = prompt.MaxOutputTokens,
            Temperature = 0.3f
        };

        ChatResponse response;

        try
        {
            response = await _chat.GetResponseAsync(messages, options, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A real shutdown, not a failure of the provider.
            throw;
        }
        catch (TaskCanceledException ex)
        {
            throw new InsightGenerationException(
                "The AI provider took too long to answer.", isTransient: true, ex);
        }
        catch (HttpRequestException ex)
        {
            throw Classify(ex);
        }
        catch (Exception ex)
        {
            throw new InsightGenerationException(
                "The AI provider failed unexpectedly.", isTransient: true, ex);
        }

        // Model output is never trusted as it arrives; a bad answer is a transient failure.
        var content = InsightOutputValidator.Validate(response.Text);
        var model = Truncate(response.ModelId ?? "unknown", MaxModelLabelLength);

        return new InsightResult(content, model, prompt.Version);
    }

    private static InsightGenerationException Classify(HttpRequestException ex)
    {
        var status = ex.StatusCode;

        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return new("The AI provider rejected the configured credentials. This is a configuration problem.", false, ex);

        if (status is HttpStatusCode.NotFound)
            return new("The AI provider does not know the configured model. Check the model name.", false, ex);

        if (status is HttpStatusCode.TooManyRequests)
            return new("The AI provider is rate limiting requests.", true, ex);

        if (status is { } code && (int)code >= 400 && (int)code < 500)
            return new($"The AI provider rejected the request ({(int)code}).", false, ex);

        // Server errors, and a provider that could not be reached at all (no status).
        return new("The AI provider could not be reached or had a server error.", true, ex);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}