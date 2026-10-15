using CreatorAnalytics.Identity.Contracts.Services;

namespace CreatorAnalytics.Integration.Tests;

public sealed class FixedOrganizationDirectory : IOrganizationDirectory
{
    private readonly IReadOnlyList<Guid> _ids;

    public FixedOrganizationDirectory(IEnumerable<Guid> ids) => _ids = ids.ToList();

    public Task<IReadOnlyList<Guid>> ListOrganizationIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_ids);
}