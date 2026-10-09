namespace CreatorAnalytics.Identity.Contracts.Services;

public sealed record OnboardingResult(bool Succeeded, Guid UserId, Guid OrganizationId, string? Error)
{
    public static OnboardingResult Success(Guid userId, Guid organizationId) =>
        new(true, userId, organizationId, null);

    public static OnboardingResult Failure(string error) =>
        new(false, Guid.Empty, Guid.Empty, error);
}

public interface IOnboardingService
{
    Task<OnboardingResult> CreateOrganizationAsync(
        string externalUserId,
        string email,
        string organizationName,
        CancellationToken cancellationToken = default);
}