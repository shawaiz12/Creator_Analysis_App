namespace CreatorAnalytics.Identity.Contracts.Services;

public sealed record MembershipInfo(Guid OrganizationId, string OrganizationName, string Role);

public sealed record OrganizationMember(Guid UserId, string Email, string Role);

public interface IMembershipQueryService
{
    Task<IReadOnlyList<MembershipInfo>> ListForUserAsync(
        string externalUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationMember>> ListMembersAsync(
        Guid tenantId, CancellationToken cancellationToken = default);
}