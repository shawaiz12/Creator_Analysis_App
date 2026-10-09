using CreatorAnalytics.Identity.Domain;
using CreatorAnalytics.Identity.Services;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Tests;

public class TenantAccessServiceTests : IdentityDatabaseTestBase
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

    [Fact]
    public async Task A_Member_Gets_Their_User_Id_And_Role()
    {
        var tenant = AddOrganization("Acme");
        var userId = AddUser("ext-1");
        AddMembership(tenant, userId, Role.Strategist);

        using var context = NewContext();
        var access = await new TenantAccessService(context).GetAccessAsync(tenant, "ext-1");

        Assert.NotNull(access);
        Assert.Equal(userId, access!.UserId);
        Assert.Equal("Strategist", access.Role);
    }

    [Fact]
    public async Task A_Member_Of_Another_Organization_Gets_Nothing()
    {
        var acme = AddOrganization("Acme");
        var globex = AddOrganization("Globex");
        var userId = AddUser("ext-1");
        AddMembership(acme, userId, Role.Admin);

        using var context = NewContext();
        var access = await new TenantAccessService(context).GetAccessAsync(globex, "ext-1");

        Assert.Null(access);
    }

    [Fact]
    public async Task An_Unknown_External_User_Gets_Nothing()
    {
        var tenant = AddOrganization("Acme");

        using var context = NewContext();
        var access = await new TenantAccessService(context).GetAccessAsync(tenant, "nobody");

        Assert.Null(access);
    }

    [Fact]
    public async Task One_User_Can_Hold_Different_Roles_In_Two_Organizations()
    {
        var acme = AddOrganization("Acme");
        var globex = AddOrganization("Globex");
        var userId = AddUser("freelancer");
        AddMembership(acme, userId, Role.Editor);
        AddMembership(globex, userId, Role.Admin);

        using var context = NewContext();
        var service = new TenantAccessService(context);

        Assert.Equal("Editor", (await service.GetAccessAsync(acme, "freelancer"))!.Role);
        Assert.Equal("Admin", (await service.GetAccessAsync(globex, "freelancer"))!.Role);
    }

    [Fact]
    public void A_User_Cannot_Have_Two_Memberships_In_One_Organization()
    {
        var tenant = AddOrganization("Acme");
        var userId = AddUser("ext-1");
        AddMembership(tenant, userId, Role.Editor);

        Assert.Throws<DbUpdateException>(() => AddMembership(tenant, userId, Role.Admin));
    }

    [Fact]
    public void The_Same_External_Id_Cannot_Be_Registered_Twice()
    {
        AddUser("duplicate");

        Assert.Throws<DbUpdateException>(() => AddUser("duplicate"));
    }

    [Fact]
    public void A_Membership_Must_Point_At_A_Real_User_And_Organization()
    {
        Assert.Throws<DbUpdateException>(
            () => AddMembership(Guid.NewGuid(), Guid.NewGuid(), Role.Admin));
    }
}