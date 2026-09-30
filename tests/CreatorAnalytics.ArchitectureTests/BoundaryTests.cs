using NetArchTest.Rules;
using System.Reflection;

namespace CreatorAnalytics.ArchitectureTests
{
    public class BoundaryTests
    {
        [Fact]
        public void Strategy_Module_Should_Not_Depend_On_Integration_Implementation()
        {
            var strategyAssembly = Assembly.Load("CreatorAnalytics.Strategy");

            var result = Types.InAssembly(strategyAssembly)
                .ShouldNot()
                .HaveDependencyOn("CreatorAnalytics.Integration")
                .GetResult();
            Assert.True(result.IsSuccessful);
        }
    }
}
