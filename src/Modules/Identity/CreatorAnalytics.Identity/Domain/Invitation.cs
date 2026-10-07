using System;

namespace CreatorAnalytics.Identity.Domain;

public class Invitation
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Email { get; private set; }
    public Role Role { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    public Invitation(Guid tenantId, string email, Role role, DateTime expiresAtUtc)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (expiresAtUtc <= DateTime.UtcNow) throw new ArgumentException("Expiry date must be in the future.", nameof(expiresAtUtc));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Email = email;
        Role = role;
        Status = InvitationStatus.Pending;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void Accept()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot accept invitation in {Status} state.");

        if (DateTime.UtcNow > ExpiresAtUtc)
        {
            Status = InvitationStatus.Expired;
            throw new InvalidOperationException("Cannot accept an expired invitation.");
        }

        Status = InvitationStatus.Accepted;
    }

    public void Revoke()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot revoke invitation in {Status} state.");

        Status = InvitationStatus.Revoked;
    }
}