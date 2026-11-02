using System.Net;
using System.Net.Http.Json;

namespace CreatorAnalytics.Api.Tests;

public sealed record GuestDraftResponse(string Content, string Model, string PromptVersion);

public sealed record GuestErrorBody(string Error);

public class GuestInsightApiTests : IClassFixture<ApiFactory>
{
    private static int _nextVisitor;
    private readonly ApiFactory _factory;

    public GuestInsightApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // Each test acts as a different visitor, so the per-visitor limit never leaks between tests.
    private static string NewVisitor()
    {
        var n = Interlocked.Increment(ref _nextVisitor);
        return $"198.51.{n / 250}.{n % 250 + 1}";
    }

    private static Dictionary<string, object?> ValidBody() => new()
    {
        ["videoTitle"] = "Guest video",
        ["durationSeconds"] = 600,
        ["transcript"] = "Welcome back. Today we edit faster.",
        ["retention"] = Enumerable.Range(1, 100).Select(i => Math.Round(1.0 - i * 0.006, 4)).ToArray(),
        ["rejectionReasons"] = new[] { "Too generic" }
    };

    private static Task<HttpResponseMessage> PostAsync(
        HttpClient client, Dictionary<string, object?> body, string visitor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/guest/insights")
        {
            Content = JsonContent.Create(body)
        };

        // Cloudflare writes the real address here. The forged header below must change nothing.
        request.Headers.Add("CF-Connecting-IP", visitor);
        request.Headers.Add("X-Forwarded-For", $"203.0.113.{Random.Shared.Next(1, 255)}");

        return client.SendAsync(request);
    }

    [Fact]
    public async Task A_Guest_Can_Get_A_Draft_Without_Signing_In()
    {
        using var client = _factory.ClientFor(null);

        var response = await PostAsync(client, ValidBody(), NewVisitor());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var draft = await response.Content.ReadFromJsonAsync<GuestDraftResponse>();
        Assert.False(string.IsNullOrWhiteSpace(draft!.Content));
    }

    [Theory]
    [InlineData("no-title")]
    [InlineData("too-short")]
    [InlineData("too-few-points")]
    [InlineData("out-of-range")]
    [InlineData("long-script")]
    [InlineData("huge-script")]
    public async Task Bad_Input_Is_Refused_Before_It_Reaches_The_Model(string problem)
    {
        var body = ValidBody();

        switch (problem)
        {
            case "no-title": body["videoTitle"] = " "; break;
            case "too-short": body["durationSeconds"] = 5; break;
            case "too-few-points": body["retention"] = new double[99]; break;
            case "out-of-range": body["retention"] = Enumerable.Repeat(1.5, 100).ToArray(); break;
            case "long-script": body["transcript"] = new string('a', 8_001); break;
            case "huge-script": body["transcript"] = new string('a', 200_000); break;
            default: throw new ArgumentException(problem);
        }

        using var client = _factory.ClientFor(null);
        var response = await PostAsync(client, body, NewVisitor());

        // 413 is also fine: it means the request was too big to even read.
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge,
            $"{problem} returned {(int)response.StatusCode}");
    }

    [Fact]
    public async Task One_Visitor_Cannot_Use_More_Than_Their_Daily_Share_Even_With_Forged_Headers()
    {
        using var client = _factory.ClientFor(null);
        var greedy = NewVisitor();

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, ValidBody(), greedy)).StatusCode);

        var refused = await PostAsync(client, ValidBody(), greedy);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        var error = await refused.Content.ReadFromJsonAsync<GuestErrorBody>();
        Assert.Contains("limit", error!.Error);

        // Someone else is not affected.
        var other = await PostAsync(client, ValidBody(), NewVisitor());
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }
}