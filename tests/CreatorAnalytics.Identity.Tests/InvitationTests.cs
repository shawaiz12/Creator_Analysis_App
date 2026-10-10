using CreatorAnalytics.Identity.Domain;

namespace CreatorAnalytics.Identity.Tests;

public class InvitationTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();

    private Invitation NewInvitation(string email = "Ana@Acme.com") =>
        new(_tenantId, email, Role.Strategist, _adminId, Now);

    [Fact]
    public void A_New_Invitation_Is_Pending_And_Expires_In_Seven_Days()
    {
        var invitation = NewInvitation();

        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Equal(Now, invitation.CreatedAtUtc);
        Assert.Equal(Now.AddDays(7), invitation.ExpiresAtUtc);
        Assert.Equal(_adminId, invitation.InvitedByUserId);
    }

    [Fact]
    public void The_Email_Is_Trimmed_And_Lowercased()
    {
        var invitation = NewInvitation("  Ana@Acme.COM ");

        Assert.Equal("ana@acme.com", invitation.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@acme.com")]
    [InlineData("ana@")]
    [InlineData("a@b@c.com")]
    public void Invalid_Emails_Are_Refused(string email)
    {
        Assert.Throws<ArgumentException>(() => NewInvitation(email));
    }

    [Fact]
    public void Accepting_With_The_Matching_Email_Works_Whatever_The_Case()
    {
        var invitation = NewInvitation("Ana@Acme.com");

        invitation.Accept("ANA@acme.COM", Now.AddDays(1));

        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
    }

    [Fact]
    public void Accepting_With_A_Different_Email_Is_Refused_And_Stays_Pending()
    {
        var invitation = NewInvitation();

        Assert.Throws<InvitationEmailMismatchException>(
            () => invitation.Accept("mallory@evil.com", Now.AddDays(1)));

        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }

    [Fact]
    public void A_Wrong_Email_Learns_Nothing_About_The_Invitation_State()
    {
        var invitation = NewInvitation();
        invitation.Accept("ana@acme.com", Now);

        // Already accepted, but a stranger must still just see "different email".
        Assert.Throws<InvitationEmailMismatchException>(
            () => invitation.Accept("mallory@evil.com", Now));
    }

    [Fact]
    public void An_Expired_Invitation_Cannot_Be_Accepted()
    {
        var invitation = NewInvitation();

        var exception = Assert.Throws<InvalidOperationException>(
            () => invitation.Accept("ana@acme.com", Now.AddDays(8)));

        Assert.Contains("expired", exception.Message);
    }

    [Fact]
    public void Accepting_Twice_Is_Refused()
    {
        var invitation = NewInvitation();
        invitation.Accept("ana@acme.com", Now);

        Assert.Throws<InvalidOperationException>(() => invitation.Accept("ana@acme.com", Now));
    }

    [Fact]
    public void A_Revoked_Invitation_Cannot_Be_Accepted()
    {
        var invitation = NewInvitation();
        invitation.Revoke();

        Assert.Throws<InvalidOperationException>(() => invitation.Accept("ana@acme.com", Now));
    }

    [Fact]
    public void Only_A_Pending_Invitation_Can_Be_Revoked()
    {
        var invitation = NewInvitation();
        invitation.Accept("ana@acme.com", Now);

        Assert.Throws<InvalidOperationException>(() => invitation.Revoke());
    }

    [Fact]
    public void An_Overdue_Pending_Invitation_Is_Marked_Expired()
    {
        var invitation = NewInvitation();

        invitation.MarkExpiredIfDue(Now.AddDays(8));

        Assert.Equal(InvitationStatus.Expired, invitation.Status);
    }

    [Fact]
    public void A_Pending_Invitation_That_Is_Not_Overdue_Stays_Pending()
    {
        var invitation = NewInvitation();

        invitation.MarkExpiredIfDue(Now.AddDays(1));

        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }
}