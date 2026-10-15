namespace CreatorAnalytics.Identity.Contracts.Services;

public interface IOrganizationDirectory
{
    /// <summary>The ids of all organizations. Used by background jobs that work tenant by tenant.</summary>
    Task<IReadOnlyList<Guid>> ListOrganizationIdsAsync(CancellationToken cancellationToken = default);
}