using System.Net;
using System.Net.Http.Json;
using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Identity.Infrastructure;
using CreatorAnalytics.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CreatorAnalytics.Api.Tests;

public sealed record InvitationCreatedResponse(Guid InvitationId);

public class InvitationApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public InvitationApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Guid> InviteAsync(
        HttpClient admin, Guid tenantId, string email, string role = "Strategist")
    {
        var response = await admin.PostAsJsonAsync(
            $"/api/{tenantId}/invitations", new { email, role });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<InvitationCreatedResponse>())!.InvitationId;
    }

    [Fact]
    public async Task An_Admin_Invites_And_The_Invited_Person_Joins()
    {
        var (tenantId, _) = await _factory.SeedOrganizationWithAdminAsync("inviter-1");
        using var admin = _factory.ClientFor("inviter-1");
        var invitationId = await InviteAsync(admin, tenantId, "newbie-1@example.com", "Strategist");

        using var newbie = _factory.ClientFor("newbie-1");
        var accepted = await newbie.PostAsync($"/api/invitations/{invitationId}/accept", null);

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var access = await new TenantAccessService(context).GetAccessAsync(tenantId, "newbie-1");

        Assert.NotNull(access);
        Assert.Equal("Strategist", access!.Role);
    }

    [Fact]
    public async Task Someone_Else_Cannot_Accept_The_Invitation()
    {
        var (tenantId, _) = await _factory.SeedOrganizationWithAdminAsync("inviter-2");
        using var admin = _factory.ClientFor("inviter-2");
        var invitationId = await InviteAsync(admin, tenantId, "newbie-2@example.com");

        using var mallory = _factory.ClientFor("mallory-2");
        var attempt = await mallory.PostAsync($"/api/invitations/{invitationId}/accept", null);

        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);

        var list = await admin.GetFromJsonAsync<List<InvitationInfo>>($"/api/{tenantId}/invitations");
        Assert.Equal("Pending", Assert.Single(list!).Status);
    }

    [Fact]
    public async Task A_Strategist_Cannot_Invite_Anyone()
    {
        var (tenantId, _) = await _factory.SeedOrganizationWithAdminAsync("inviter-3");
        _factory.Access.AddMember(tenantId, "strategist-3", Guid.NewGuid(), "Strategist");
        using var strategist = _factory.ClientFor("strategist-3");

        var response = await strategist.PostAsJsonAsync(
            $"/api/{tenantId}/invitations",
            new { email = "someone@example.com", role = "Editor" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Another_Organizations_Admin_Cannot_Revoke_The_Invitation()
    {
        var (tenantA, _) = await _factory.SeedOrganizationWithAdminAsync("inviter-4a");
        var (tenantB, _) = await _factory.SeedOrganizationWithAdminAsync("inviter-4b");
        using var adminA = _factory.ClientFor("inviter-4a");
        using var adminB = _factory.ClientFor("inviter-4b");
        var invitationId = await InviteAsync(adminA, tenantA, "newbie-4@example.com");

        var attempt = await adminB.PostAsync(
            $"/api/{tenantB}/invitations/{invitationId}/revoke", null);

        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);

        var list = await adminA.GetFromJsonAsync<List<InvitationInfo>>($"/api/{tenantA}/invitations");
        Assert.Equal("Pending", Assert.Single(list!).Status);
    }

    [Fact]
    public async Task Accepting_Requires_Sign_In()
    {
        using var anonymous = _factory.ClientFor(null);

        var response = await anonymous.PostAsync($"/api/invitations/{Guid.NewGuid()}/accept", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inviting_The_Same_Email_Twice_Returns_409()
    {
        var (tenantId, _) = await _factory.SeedOrganizationWithAdminAsync("inviter-6");
        using var admin = _factory.ClientFor("inviter-6");
        await InviteAsync(admin, tenantId, "newbie-6@example.com");

        var second = await admin.PostAsJsonAsync(
            $"/api/{tenantId}/invitations",
            new { email = "newbie-6@example.com", role = "Editor" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}