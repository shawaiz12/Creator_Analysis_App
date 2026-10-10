using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Identity.Domain;
using CreatorAnalytics.Identity.Services;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Tests;

public class InvitationServiceTests : IdentityDatabaseTestBase
{
    private static readonly DateTime Now = new(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

    private Guid AddOrganization(string name)
    {
        using var context = NewContext();
        var organization = new Organization(name);
        context.Organizations.Add(organization);
        context.SaveChanges();
        return organization.Id;
    }

    private Guid AddUser(string externalId, string? email = null)
    {
        using var context = NewContext();
        var user = new User(externalId, email ?? $"{externalId}@example.com");
        context.Users.Add(user);
        context.SaveChanges();
        return user.Id;
    }

    private void AddMembership(Guid tenantId, Guid userId, Role role)
    {
        using var context = NewContext();
        context.TenantMemberships.Add(new TenantMembership(tenantId, userId, role));
        context.SaveChanges();
    }

    private (Guid TenantId, Guid AdminId) NewOrganizationWithAdmin(
        string name = "Acme", string adminExternalId = "admin")
    {
        var tenant = AddOrganization(name);
        var admin = AddUser(adminExternalId);
        AddMembership(tenant, admin, Role.Admin);
        return (tenant, admin);
    }

    private async Task<InvitationResult> Create(
        Guid tenantId, Guid adminId, string email, string role = "Strategist", DateTime? at = null)
    {
        using var context = NewContext();
        return await new InvitationService(context)
            .CreateAsync(tenantId, adminId, email, role, at ?? Now);
    }

    private async Task<InvitationResult> Accept(
        Guid invitationId, string externalId, string email, DateTime? at = null)
    {
        using var context = NewContext();
        return await new InvitationService(context)
            .AcceptAsync(invitationId, externalId, email, at ?? Now.AddDays(1));
    }

    private async Task<InvitationResult> Revoke(Guid tenantId, Guid invitationId)
    {
        using var context = NewContext();
        return await new InvitationService(context).RevokeAsync(tenantId, invitationId);
    }

    private async Task<IReadOnlyList<InvitationInfo>> List(Guid tenantId, DateTime? at = null)
    {
        using var context = NewContext();
        return await new InvitationService(context).ListAsync(tenantId, at ?? Now);
    }

    [Fact]
    public async Task Create_Stores_A_Pending_Invitation_With_A_Normalized_Email()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();

        var result = await Create(tenant, admin, "Ana@Example.com", "editor");

        Assert.True(result.Succeeded);
        var item = Assert.Single(await List(tenant));
        Assert.Equal("ana@example.com", item.Email);
        Assert.Equal("Editor", item.Role);
        Assert.Equal("Pending", item.Status);
    }

    [Fact]
    public async Task Bad_Emails_And_Unknown_Roles_Are_Invalid()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();

        Assert.Equal(InvitationOutcome.Invalid, (await Create(tenant, admin, "not-an-email")).Outcome);
        Assert.Equal(InvitationOutcome.Invalid, (await Create(tenant, admin, "ana@example.com", "Overlord")).Outcome);
        Assert.Empty(await List(tenant));
    }

    [Fact]
    public async Task Inviting_An_Existing_Member_Is_A_Conflict()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();
        var ana = AddUser("ana-ext", "ana@example.com");
        AddMembership(tenant, ana, Role.Editor);

        var result = await Create(tenant, admin, "ANA@example.com");

        Assert.Equal(InvitationOutcome.Conflict, result.Outcome);
    }

    [Fact]
    public async Task A_Second_Open_Invitation_For_The_Same_Email_Is_A_Conflict()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();
        await Create(tenant, admin, "ana@example.com");

        var second = await Create(tenant, admin, "Ana@Example.com");

        Assert.Equal(InvitationOutcome.Conflict, second.Outcome);
    }

    [Fact]
    public async Task An_Expired_Invitation_Does_Not_Block_A_New_One()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();
        await Create(tenant, admin, "ana@example.com");

        var later = Now.AddDays(8);
        var second = await Create(tenant, admin, "ana@example.com", at: later);

        Assert.True(second.Succeeded);
        var list = await List(tenant, later);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, i => i.Status == "Expired");
        Assert.Contains(list, i => i.Status == "Pending");
    }

    [Fact]
    public async Task Accepting_Creates_The_User_And_The_Membership()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();
        var created = await Create(tenant, admin, "ana@example.com", "Strategist");

        var accepted = await Accept(created.InvitationId!.Value, "ana-ext", "Ana@Example.com");

        Assert.True(accepted.Succeeded);
        Assert.Equal(tenant, accepted.OrganizationId);

        using var context = NewContext();
        var access = await new TenantAccessService(context).GetAccessAsync(tenant, "ana-ext");
        Assert.NotNull(access);
        Assert.Equal("Strategist", access!.Role);
        Assert.Equal("Accepted", Assert.Single(await List(tenant)).Status);
    }

    [Fact]
    public async Task Accepting_Reuses_An_Existing_User_Instead_Of_Duplicating_Them()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();
        AddUser("ana-ext", "ana@example.com");
        var created = await Create(tenant, admin, "ana@example.com", "Editor");

        var accepted = await Accept(created.InvitationId!.Value, "ana-ext", "ana@example.com");

        Assert.True(accepted.Succeeded);
        using var context = NewContext();
        Assert.Equal(2, await context.Users.CountAsync());
        Assert.Equal(2, await context.TenantMemberships.CountAsync());
    }

    [Fact]
    public async Task A_Wrong_Email_Looks_Like_An_Unknown_Invitation_And_Changes_Nothing()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();
        var created = await Create(tenant, admin, "ana@example.com");

        var attempt = await Accept(created.InvitationId!.Value, "mallory-ext", "mallory@evil.com");

        Assert.Equal(InvitationOutcome.NotFound, attempt.Outcome);
        Assert.Equal("Pending", Assert.Single(await List(tenant)).Status);

        using var context = NewContext();
        Assert.False(await context.Users.AnyAsync(u => u.ExternalId == "mallory-ext"));
    }

    [Fact]
    public async Task An_Unknown_Invitation_Id_Is_NotFound()
    {
        var attempt = await Accept(Guid.NewGuid(), "ana-ext", "ana@example.com");

        Assert.Equal(InvitationOutcome.NotFound, attempt.Outcome);
    }

    [Fact]
    public async Task An_Expired_Invitation_Cannot_Be_Accepted()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();
        var created = await Create(tenant, admin, "ana@example.com");

        var attempt = await Accept(created.InvitationId!.Value, "ana-ext", "ana@example.com", Now.AddDays(8));

        Assert.Equal(InvitationOutcome.Conflict, attempt.Outcome);
    }

    [Fact]
    public async Task Accepting_Twice_Is_A_Conflict_The_Second_Time()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();
        var created = await Create(tenant, admin, "ana@example.com");
        var id = created.InvitationId!.Value;

        var first = await Accept(id, "ana-ext", "ana@example.com");
        var second = await Accept(id, "ana-ext", "ana@example.com");

        Assert.True(first.Succeeded);
        Assert.Equal(InvitationOutcome.Conflict, second.Outcome);
    }

    [Fact]
    public async Task A_Revoked_Invitation_Cannot_Be_Accepted()
    {
        var (tenant, admin) = NewOrganizationWithAdmin();
        var created = await Create(tenant, admin, "ana@example.com");
        var id = created.InvitationId!.Value;

        Assert.True((await Revoke(tenant, id)).Succeeded);
        var attempt = await Accept(id, "ana-ext", "ana@example.com");

        Assert.Equal(InvitationOutcome.Conflict, attempt.Outcome);
    }

    [Fact]
    public async Task One_Organization_Cannot_Revoke_Anothers_Invitation()
    {
        var (acme, acmeAdmin) = NewOrganizationWithAdmin("Acme", "admin-a");
        var (globex, _) = NewOrganizationWithAdmin("Globex", "admin-b");
        var created = await Create(acme, acmeAdmin, "ana@example.com");

        var attempt = await Revoke(globex, created.InvitationId!.Value);

        Assert.Equal(InvitationOutcome.NotFound, attempt.Outcome);
        Assert.Equal("Pending", Assert.Single(await List(acme)).Status);
    }

    [Fact]
    public async Task Listing_Only_Shows_The_Organizations_Own_Invitations()
    {
        var (acme, acmeAdmin) = NewOrganizationWithAdmin("Acme", "admin-a");
        var (globex, globexAdmin) = NewOrganizationWithAdmin("Globex", "admin-b");
        await Create(acme, acmeAdmin, "for-acme@example.com");
        await Create(globex, globexAdmin, "for-globex@example.com");

        var item = Assert.Single(await List(acme));

        Assert.Equal("for-acme@example.com", item.Email);
    }
}