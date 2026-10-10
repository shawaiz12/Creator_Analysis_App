namespace CreatorAnalytics.Identity.Contracts.Services;

public enum InvitationOutcome
{
    Succeeded,
    NotFound,
    Conflict,
    Invalid
}

public sealed record InvitationResult(
    InvitationOutcome Outcome, Guid? InvitationId, Guid? OrganizationId, string? Error)
{
    public bool Succeeded => Outcome == InvitationOutcome.Succeeded;

    public static InvitationResult Success(Guid invitationId, Guid organizationId) =>
        new(InvitationOutcome.Succeeded, invitationId, organizationId, null);

    public static InvitationResult NotFound() =>
        new(InvitationOutcome.NotFound, null, null, "Invitation not found.");

    public static InvitationResult Conflict(string error) =>
        new(InvitationOutcome.Conflict, null, null, error);

    public static InvitationResult Invalid(string error) =>
        new(InvitationOutcome.Invalid, null, null, error);
}

public sealed record InvitationInfo(
    Guid Id, string Email, string Role, string Status, DateTime CreatedAtUtc, DateTime ExpiresAtUtc);

public interface IInvitationService
{
    Task<InvitationResult> CreateAsync(
        Guid tenantId, Guid invitedByUserId, string email, string role, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvitationInfo>> ListAsync(
        Guid tenantId, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<InvitationResult> RevokeAsync(
        Guid tenantId, Guid invitationId, CancellationToken cancellationToken = default);

    Task<InvitationResult> AcceptAsync(
        Guid invitationId, string externalUserId, string email, DateTime nowUtc,
        CancellationToken cancellationToken = default);
}