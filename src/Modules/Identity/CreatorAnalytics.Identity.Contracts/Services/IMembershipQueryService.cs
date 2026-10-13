namespace CreatorAnalytics.Identity.Contracts.Services;

public sealed record MembershipInfo(Guid OrganizationId, string OrganizationName, string Role);

public interface IMembershipQueryService
{
    Task<IReadOnlyList<MembershipInfo>> ListForUserAsync(
        string externalUserId, CancellationToken cancellationToken = default);
}