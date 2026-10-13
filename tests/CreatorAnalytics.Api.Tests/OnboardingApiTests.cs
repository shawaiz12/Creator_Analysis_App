using System.Net;
using System.Net.Http.Json;
using CreatorAnalytics.Identity.Infrastructure;
using CreatorAnalytics.Identity.Services;
using Microsoft.Extensions.DependencyInjection;
using CreatorAnalytics.Identity.Contracts.Services;

namespace CreatorAnalytics.Api.Tests;

public sealed record OrganizationCreatedResponse(Guid OrganizationId);
public sealed record MeResponse(string? Email, List<MembershipInfo> Organizations);

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

    [Fact]
    public async Task Me_Requires_Sign_In()
    {
        using var anonymous = _factory.ClientFor(null);

        var response = await anonymous.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_Lists_The_Organization_Created_By_Onboarding()
    {
        using var client = _factory.ClientFor("me-ext");
        var created = await client.PostAsJsonAsync(
            "/api/onboarding/organizations", new { name = "Me Studio" });
        var organization = (await created.Content.ReadFromJsonAsync<OrganizationCreatedResponse>())!;

        var me = await client.GetFromJsonAsync<MeResponse>("/api/me");

        var membership = Assert.Single(me!.Organizations);
        Assert.Equal(organization.OrganizationId, membership.OrganizationId);
        Assert.Equal("Me Studio", membership.OrganizationName);
        Assert.Equal("Admin", membership.Role);
        Assert.Equal("me-ext@example.com", me.Email);
    }
}