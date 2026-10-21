using CreatorAnalytics.SharedKernel.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CreatorAnalytics.Api.Tests;

public class MigratorApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public MigratorApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Every_Module_Registers_A_Migrator()
    {
        using var scope = _factory.Services.CreateScope();

        var names = scope.ServiceProvider.GetServices<IDatabaseMigrator>()
            .Select(m => m.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(new[] { "audit", "identity", "integration", "strategy" }, names);
    }

    [Fact]
    public async Task Running_The_Migrators_Again_Is_Safe()
    {
        using var scope = _factory.Services.CreateScope();

        foreach (var migrator in scope.ServiceProvider.GetServices<IDatabaseMigrator>())
            await migrator.MigrateAsync();
    }
}