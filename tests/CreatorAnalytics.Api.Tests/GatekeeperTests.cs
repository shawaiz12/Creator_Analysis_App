using System.Net;

namespace CreatorAnalytics.Api.Tests;

public class GatekeeperTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GatekeeperTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Access.AddMember(_tenantId, "admin-ext", Guid.NewGuid(), "Admin");
    }

    [Fact]
    public async Task Request_Without_Sign_In_Gets_401()
    {
        using var client = _factory.ClientFor(null);

        var response = await client.PostAsync($"/api/{_tenantId}/strategies", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signed_In_Non_Member_Gets_404()
    {
        using var client = _factory.ClientFor("stranger-ext");

        var response = await client.PostAsync($"/api/{_tenantId}/strategies", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_Tenant_Id_Gets_404()
    {
        using var client = _factory.ClientFor("admin-ext");

        var response = await client.PostAsync("/api/not-a-guid/strategies", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_Member_Passes_The_Gatekeeper()
    {
        using var client = _factory.ClientFor("admin-ext");

        var response = await client.PostAsync($"/api/{_tenantId}/strategies", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}