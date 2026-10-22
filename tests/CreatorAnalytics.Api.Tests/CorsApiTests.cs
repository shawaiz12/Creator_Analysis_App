using System.Net;

namespace CreatorAnalytics.Api.Tests;

public class CorsApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CorsApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, $"/api/{Guid.NewGuid()}/strategies");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        return request;
    }

    private static string? AllowedOriginOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values)
            ? values.FirstOrDefault()
            : null;

    [Fact]
    public async Task The_Web_App_Origin_Is_Allowed_To_Make_The_Preflight_Request()
    {
        using var client = _factory.ClientFor(null);

        var response = await client.SendAsync(Preflight("http://localhost:5173"));

        Assert.Equal("http://localhost:5173", AllowedOriginOf(response));
    }

    [Fact]
    public async Task An_Unknown_Origin_Gets_No_Permission()
    {
        using var client = _factory.ClientFor(null);

        var response = await client.SendAsync(Preflight("https://evil.example"));

        Assert.Null(AllowedOriginOf(response));
    }

    [Fact]
    public async Task A_Normal_Request_From_The_Web_App_Carries_The_Permission_Header()
    {
        using var client = _factory.ClientFor(null);
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:5173", AllowedOriginOf(response));
    }
}