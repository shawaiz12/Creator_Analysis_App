using System.Net;
using System.Net.Http.Json;
using CreatorAnalytics.Identity.Infrastructure;
using CreatorAnalytics.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CreatorAnalytics.Api.Tests;

public sealed record OrganizationCreatedResponse(Guid OrganizationId);

public class OnboardingApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public OnboardingApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Onboarding_Requires_Sign_In()
    {
        using var client = _factory.ClientFor(null);

        var response = await client.PostAsJsonAsync("/api/onboarding/organizations", new { name = "Acme" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_New_User_Creates_An_Organization_And_Becomes_Its_Admin()
    {
        using var client = _factory.ClientFor("newcomer-ext");

        var response = await client.PostAsJsonAsync(
            "/api/onboarding/organizations", new { name = "Acme Studio" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<OrganizationCreatedResponse>())!;

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var access = await new TenantAccessService(context)
            .GetAccessAsync(created.OrganizationId, "newcomer-ext");

        Assert.NotNull(access);
        Assert.Equal("Admin", access!.Role);
    }

    [Fact]
    public async Task A_Blank_Organization_Name_Returns_400()
    {
        using var client = _factory.ClientFor("blank-name-ext");

        var response = await client.PostAsJsonAsync("/api/onboarding/organizations", new { name = "  " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}