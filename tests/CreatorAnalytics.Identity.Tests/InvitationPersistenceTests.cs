using CreatorAnalytics.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Tests;

public class InvitationPersistenceTests : IdentityDatabaseTestBase
{
    private static readonly DateTime Now = DateTime.UtcNow;

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

    private Invitation Save(Invitation invitation)
    {
        using var context = NewContext();
        context.Invitations.Add(invitation);
        context.SaveChanges();
        return invitation;
    }

    [Fact]
    public void An_Invitation_Round_Trips_With_Its_Normalized_Email()
    {
        var tenant = AddOrganization("Acme");
        var admin = AddUser("admin");
        var saved = Save(new Invitation(tenant, "  Ana@Acme.com ", Role.Editor, admin, Now));

        using var context = NewContext();
        var loaded = context.Invitations.Single(i => i.Id == saved.Id);

        Assert.Equal("ana@acme.com", loaded.Email);
        Assert.Equal(Role.Editor, loaded.Role);
        Assert.Equal(InvitationStatus.Pending, loaded.Status);
        Assert.Equal(admin, loaded.InvitedByUserId);
    }

    [Fact]
    public void A_Second_Open_Invitation_For_The_Same_Email_Is_Refused()
    {
        var tenant = AddOrganization("Acme");
        var admin = AddUser("admin");
        Save(new Invitation(tenant, "ana@acme.com", Role.Editor, admin, Now));

        Assert.Throws<DbUpdateException>(
            () => Save(new Invitation(tenant, "ANA@acme.com", Role.Strategist, admin, Now)));
    }

    [Fact]
    public void The_Same_Email_Can_Be_Invited_Again_After_A_Revocation()
    {
        var tenant = AddOrganization("Acme");
        var admin = AddUser("admin");
        var first = Save(new Invitation(tenant, "ana@acme.com", Role.Editor, admin, Now));

        using (var context = NewContext())
        {
            var toRevoke = context.Invitations.Single(i => i.Id == first.Id);
            toRevoke.Revoke();
            context.SaveChanges();
        }

        var second = Save(new Invitation(tenant, "ana@acme.com", Role.Strategist, admin, Now));

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void The_Same_Email_Can_Be_Invited_To_Two_Organizations()
    {
        var acme = AddOrganization("Acme");
        var globex = AddOrganization("Globex");
        var admin = AddUser("admin");

        Save(new Invitation(acme, "ana@example.com", Role.Editor, admin, Now));
        var second = Save(new Invitation(globex, "ana@example.com", Role.Editor, admin, Now));

        Assert.Equal(globex, second.TenantId);
    }
}