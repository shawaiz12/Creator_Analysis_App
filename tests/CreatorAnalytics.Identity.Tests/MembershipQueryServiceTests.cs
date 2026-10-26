using CreatorAnalytics.Identity.Domain;
using CreatorAnalytics.Identity.Services;
using CreatorAnalytics.Identity.Contracts.Services;

namespace CreatorAnalytics.Identity.Tests;

public class MembershipQueryServiceTests : IdentityDatabaseTestBase
{
    private Guid AddOrganization(string name)
    {
        using var context = NewContext();
        var organization = new Organization(name);
        context.Organizations.Add(organization);
        context.SaveChanges();
        return organization.Id;
    }

    private Guid AddUser(string externalId)
    {
        using var context = NewContext();
        var user = new User(externalId, $"{externalId}@example.com");
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

    private async Task<IReadOnlyList<CreatorAnalytics.Identity.Contracts.Services.MembershipInfo>> ListFor(
        string externalId)
    {
        using var context = NewContext();
        return await new MembershipQueryService(context).ListForUserAsync(externalId);
    }

    [Fact]
    public async Task A_User_Sees_Every_Organization_With_Their_Role_In_Each()
    {
        var acme = AddOrganization("Acme");
        var globex = AddOrganization("Globex");
        var freelancer = AddUser("freelancer");
        AddMembership(acme, freelancer, Role.Editor);
        AddMembership(globex, freelancer, Role.Admin);

        var result = await ListFor("freelancer");

        Assert.Equal(2, result.Count);
        Assert.Equal("Acme", result[0].OrganizationName);
        Assert.Equal("Editor", result[0].Role);
        Assert.Equal("Globex", result[1].OrganizationName);
        Assert.Equal("Admin", result[1].Role);
    }

    [Fact]
    public async Task An_Unknown_User_Sees_Nothing()
    {
        AddOrganization("Acme");

        Assert.Empty(await ListFor("nobody"));
    }

    [Fact]
    public async Task A_User_Never_Sees_Other_Peoples_Memberships()
    {
        var acme = AddOrganization("Acme");
        var globex = AddOrganization("Globex");
        var ana = AddUser("ana");
        var ben = AddUser("ben");
        AddMembership(acme, ana, Role.Admin);
        AddMembership(globex, ben, Role.Admin);

        var result = await ListFor("ana");

        var only = Assert.Single(result);
        Assert.Equal("Acme", only.OrganizationName);
    }
    private async Task<IReadOnlyList<OrganizationMember>> ListMembers(Guid tenantId)
    {
        using var context = NewContext();
        return await new MembershipQueryService(context).ListMembersAsync(tenantId);
    }

    [Fact]
    public async Task Members_Are_Listed_With_Email_And_Role_Sorted_By_Email()
    {
        var acme = AddOrganization("Acme");
        var zedId = AddUser("zed");
        var anaId = AddUser("ana");
        AddMembership(acme, zedId, Role.Editor);
        AddMembership(acme, anaId, Role.Admin);

        var result = await ListMembers(acme);

        Assert.Equal(2, result.Count);
        Assert.Equal(anaId, result[0].UserId);
        Assert.Equal("ana@example.com", result[0].Email);
        Assert.Equal("Admin", result[0].Role);
        Assert.Equal(zedId, result[1].UserId);
        Assert.Equal("zed@example.com", result[1].Email);
        Assert.Equal("Editor", result[1].Role);
    }

    [Fact]
    public async Task An_Organization_Never_Lists_Another_Organizations_Members()
    {
        var acme = AddOrganization("Acme");
        var globex = AddOrganization("Globex");
        AddMembership(acme, AddUser("ana"), Role.Admin);
        AddMembership(globex, AddUser("ben"), Role.Admin);

        var result = await ListMembers(acme);

        var only = Assert.Single(result);
        Assert.Equal("ana@example.com", only.Email);
    }

    [Fact]
    public async Task An_Organization_Without_Members_Lists_Nobody()
    {
        var empty = AddOrganization("Empty");

        Assert.Empty(await ListMembers(empty));
    }
}