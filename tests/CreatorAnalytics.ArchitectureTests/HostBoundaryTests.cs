using CreatorAnalytics.Api.BackgroundServices;
using NetArchTest.Rules;

namespace CreatorAnalytics.ArchitectureTests;

public class HostBoundaryTests
{
    [Theory]
    [InlineData("CreatorAnalytics.Strategy.Domain")]
    [InlineData("CreatorAnalytics.Strategy.Infrastructure")]
    [InlineData("CreatorAnalytics.Identity.Domain")]
    [InlineData("CreatorAnalytics.Identity.Infrastructure")]
    [InlineData("CreatorAnalytics.Audit.Domain")]
    [InlineData("CreatorAnalytics.Audit.Infrastructure")]
    public void The_Api_Host_Does_Not_Touch_Module_Internals(string moduleInternalNamespace)
    {
        var result = Types
            .InAssembly(typeof(OutboxDispatcher).Assembly)
            .That().ResideInNamespaceStartingWith("CreatorAnalytics.Api")
            .ShouldNot().HaveDependencyOn(moduleInternalNamespace)
            .GetResult();

        var offenders = result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);

        Assert.True(
            result.IsSuccessful,
            $"The Api host depends on {moduleInternalNamespace} in: {offenders}");
    }
}