using CreatorAnalytics.Identity.Domain;
using CreatorAnalytics.Identity.Services;

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
}