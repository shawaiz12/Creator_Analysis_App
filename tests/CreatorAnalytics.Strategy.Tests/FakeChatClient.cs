using Microsoft.Extensions.AI;

namespace CreatorAnalytics.Strategy.Tests;

/// <summary>A stand-in language model whose answers and failures the test controls.</summary>
public sealed class FakeChatClient : IChatClient
{
    public string? ReplyText { get; set; }

    public string? ModelId { get; set; } = "fake-model";

    public Exception? ThrowOnCall { get; set; }

    public List<ChatMessage> LastMessages { get; private set; } = new();

    public ChatOptions? LastOptions { get; private set; }

    public int Calls { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastMessages = messages.ToList();
        LastOptions = options;

        if (ThrowOnCall is not null)
            throw ThrowOnCall;

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, ReplyText ?? string.Empty))
        {
            ModelId = ModelId
        };

        return Task.FromResult(response);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}