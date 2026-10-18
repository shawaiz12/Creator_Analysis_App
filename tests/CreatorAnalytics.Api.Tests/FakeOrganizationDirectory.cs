using CreatorAnalytics.Identity.Contracts.Services;

namespace CreatorAnalytics.Api.Tests;

public sealed class FakeOrganizationDirectory : IOrganizationDirectory
{
    private readonly FakeTenantAccessService _access;

    public FakeOrganizationDirectory(FakeTenantAccessService access) => _access = access;

    public Task<IReadOnlyList<Guid>> ListOrganizationIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_access.KnownTenantIds());
}