using System.Net;
using System.Net.Http.Json;
using CreatorAnalytics.Identity.Contracts.Services;

namespace CreatorAnalytics.Api.Tests;

public class MembersApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public MembersApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task An_Admin_Lists_Their_Organizations_Members()
    {
        var (tenantId, adminUserId) = await _factory.SeedOrganizationWithAdminAsync("members-1");
        using var admin = _factory.ClientFor("members-1");

        var members = await admin.GetFromJsonAsync<List<OrganizationMember>>($"/api/{tenantId}/members");

        var only = Assert.Single(members!);
        Assert.Equal(adminUserId, only.UserId);
        Assert.Equal("members-1@example.com", only.Email);
        Assert.Equal("Admin", only.Role);
    }

    [Fact]
    public async Task The_List_Never_Includes_Another_Organizations_Members()
    {
        var (tenantA, _) = await _factory.SeedOrganizationWithAdminAsync("members-2a");
        await _factory.SeedOrganizationWithAdminAsync("members-2b");
        using var adminA = _factory.ClientFor("members-2a");

        var members = await adminA.GetFromJsonAsync<List<OrganizationMember>>($"/api/{tenantA}/members");

        var only = Assert.Single(members!);
        Assert.Equal("members-2a@example.com", only.Email);
    }

    [Fact]
    public async Task Another_Organizations_Admin_Gets_404()
    {
        var (tenantA, _) = await _factory.SeedOrganizationWithAdminAsync("members-3a");
        await _factory.SeedOrganizationWithAdminAsync("members-3b");
        using var adminB = _factory.ClientFor("members-3b");

        var response = await adminB.GetAsync($"/api/{tenantA}/members");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Only_Admins_Can_List_Members()
    {
        var (tenantId, _) = await _factory.SeedOrganizationWithAdminAsync("members-4");
        _factory.Access.AddMember(tenantId, "strategist-4", Guid.NewGuid(), "Strategist");
        _factory.Access.AddMember(tenantId, "editor-4", Guid.NewGuid(), "Editor");
        using var strategist = _factory.ClientFor("strategist-4");
        using var editor = _factory.ClientFor("editor-4");

        var asStrategist = await strategist.GetAsync($"/api/{tenantId}/members");
        var asEditor = await editor.GetAsync($"/api/{tenantId}/members");

        Assert.Equal(HttpStatusCode.Forbidden, asStrategist.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, asEditor.StatusCode);
    }
}