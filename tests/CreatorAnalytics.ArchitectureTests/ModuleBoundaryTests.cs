using System.Reflection;

namespace CreatorAnalytics.ArchitectureTests;

public class ModuleBoundaryTests
{
    private static readonly string[] Modules = { "Identity", "Integration", "Strategy", "Audit" };

    [Theory]
    [InlineData("Identity")]
    [InlineData("Integration")]
    [InlineData("Strategy")]
    [InlineData("Audit")]
    public void A_Module_Never_Uses_Another_Modules_Implementation(string module)
    {
        var assembly = Assembly.Load($"CreatorAnalytics.{module}");

        var used = assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        var otherImplementations = Modules
            .Where(m => m != module)
            .Select(m => $"CreatorAnalytics.{m}");

        var offenders = used.Intersect(otherImplementations).ToList();

        Assert.True(
            offenders.Count == 0,
            $"Module '{module}' uses the implementation of: {string.Join(", ", offenders)}. " +
            "Modules may only use each other's Contracts.");
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Integration")]
    [InlineData("Strategy")]
    [InlineData("Audit")]
    public void A_Contracts_Assembly_Uses_No_Module(string module)
    {
        var assembly = Assembly.Load($"CreatorAnalytics.{module}.Contracts");

        var offenders = assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => name.StartsWith("CreatorAnalytics.") && name != "CreatorAnalytics.SharedKernel")
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"The {module} Contracts assembly uses: {string.Join(", ", offenders)}. " +
            "Contracts must stay free of module code.");
    }
}