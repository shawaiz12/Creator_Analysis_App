using System.Net;
using CreatorAnalytics.Api.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CreatorAnalytics.Api.Tests;

public sealed class FallbackChatClientTests
{
    private static readonly ChatMessage[] Messages = { new(ChatRole.User, "hello") };

    [Fact]
    public async Task Uses_the_primary_model_when_it_answers()
    {
        var primary = Answering("primary-model");
        var fallback = Answering("fallback-model");
        var sut = new FallbackChatClient(primary, fallback, NullLogger<FallbackChatClient>.Instance);

        var response = await sut.GetResponseAsync(Messages);

        Assert.Equal("primary-model", response.ModelId);
        Assert.Equal(1, primary.Calls);
        Assert.Equal(0, fallback.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    public async Task Falls_back_on_a_provider_side_problem(HttpStatusCode status)
    {
        var primary = Failing(status);
        var fallback = Answering("fallback-model");
        var sut = new FallbackChatClient(primary, fallback, NullLogger<FallbackChatClient>.Instance);

        var response = await sut.GetResponseAsync(Messages);

        Assert.Equal("fallback-model", response.ModelId);
        Assert.Equal(1, primary.Calls);
        Assert.Equal(1, fallback.Calls);
    }

    [Fact]
    public async Task Falls_back_when_the_provider_gives_no_answer_at_all()
    {
        var primary = Failing(null);
        var fallback = Answering("fallback-model");
        var sut = new FallbackChatClient(primary, fallback, NullLogger<FallbackChatClient>.Instance);

        var response = await sut.GetResponseAsync(Messages);

        Assert.Equal("fallback-model", response.ModelId);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Does_not_fall_back_on_configuration_errors(HttpStatusCode status)
    {
        var primary = Failing(status);
        var fallback = Answering("fallback-model");
        var sut = new FallbackChatClient(primary, fallback, NullLogger<FallbackChatClient>.Instance);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetResponseAsync(Messages));

        Assert.Equal(status, error.StatusCode);
        Assert.Equal(0, fallback.Calls);
    }

    [Fact]
    public async Task Reports_the_fallbacks_error_when_both_models_fail()
    {
        var primary = Failing(HttpStatusCode.ServiceUnavailable);
        var fallback = Failing(HttpStatusCode.TooManyRequests);
        var sut = new FallbackChatClient(primary, fallback, NullLogger<FallbackChatClient>.Instance);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetResponseAsync(Messages));

        Assert.Equal(HttpStatusCode.TooManyRequests, error.StatusCode);
    }

    private static StubChatClient Answering(string model) =>
        new(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, "draft")) { ModelId = model });

    private static StubChatClient Failing(HttpStatusCode? status) =>
        new(() => throw new HttpRequestException("failure", null, status));

    private sealed class StubChatClient : IChatClient
    {
        private readonly Func<ChatResponse> _respond;

        public StubChatClient(Func<ChatResponse> respond) => _respond = respond;

        public int Calls { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_respond());
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
}