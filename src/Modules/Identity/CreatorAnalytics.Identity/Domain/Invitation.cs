using System;

namespace CreatorAnalytics.Identity.Domain;

public class Invitation
{
    public static readonly TimeSpan DefaultValidity = TimeSpan.FromDays(7);

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Email { get; private set; }
    public Role Role { get; private set; }
    public InvitationStatus Status { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    private Invitation()
    {
        Email = string.Empty;
    }

    public Invitation(Guid tenantId, string email, Role role, Guid invitedByUserId, DateTime nowUtc)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (invitedByUserId == Guid.Empty) throw new ArgumentException("InvitedByUserId is required.", nameof(invitedByUserId));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Email = NormalizeEmail(email);
        Role = role;
        Status = InvitationStatus.Pending;
        InvitedByUserId = invitedByUserId;
        CreatedAtUtc = nowUtc;
        ExpiresAtUtc = nowUtc.Add(DefaultValidity);
    }

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        var normalized = email.Trim().ToLowerInvariant();
        var at = normalized.IndexOf('@');

        var looksValid = at > 0
                         && at < normalized.Length - 1
                         && normalized.IndexOf('@', at + 1) < 0
                         && normalized.Length <= 255;

        if (!looksValid)
            throw new ArgumentException("Email address is not valid.", nameof(email));

        return normalized;
    }

    public bool IsExpired(DateTime nowUtc) => nowUtc > ExpiresAtUtc;

    public bool EmailMatches(string? email) =>
        !string.IsNullOrWhiteSpace(email) && email.Trim().ToLowerInvariant() == Email;

    public void Accept(string signedInEmail, DateTime nowUtc)
    {
        // Identity first, state second: a stranger learns nothing about the invitation's state.
        if (!EmailMatches(signedInEmail))
            throw new InvitationEmailMismatchException();

        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot accept invitation in {Status} state.");

        if (IsExpired(nowUtc))
            throw new InvalidOperationException("Cannot accept an expired invitation.");

        Status = InvitationStatus.Accepted;
    }

    public void Revoke()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot revoke invitation in {Status} state.");

        Status = InvitationStatus.Revoked;
    }

    // Lets an old, unanswered invitation stop blocking a fresh one for the same email.
    public void MarkExpiredIfDue(DateTime nowUtc)
    {
        if (Status == InvitationStatus.Pending && IsExpired(nowUtc))
            Status = InvitationStatus.Expired;
    }
}

public sealed class InvitationEmailMismatchException : Exception
{
    public InvitationEmailMismatchException()
        : base("This invitation was sent to a different email address.")
    {
    }
}