namespace CreatorAnalytics.ArchitectureTests;

public class QueryFilterGuardTests
{
    private static readonly string[] Forbidden =
    {
        "IgnoreQueryFilters",
        "FromSql",
        "ExecuteSql"
    };

    [Fact]
    public void Source_Code_Never_Bypasses_Tenant_Filters_Or_Uses_Raw_Sql()
    {
        var srcFolder = Path.Combine(RepoLocator.FindRoot(), "src");
        var sep = Path.DirectorySeparatorChar;

        var offences = Directory
            .EnumerateFiles(srcFolder, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{sep}obj{sep}")
                        && !path.Contains($"{sep}bin{sep}")
                        && !path.Contains($"{sep}Migrations{sep}"))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => (path, line, number: index + 1)))
            .Where(x => Forbidden.Any(word => x.line.Contains(word)))
            .Select(x => $"{Path.GetRelativePath(srcFolder, x.path)}:{x.number} -> {x.line.Trim()}")
            .ToList();

        Assert.True(
            offences.Count == 0,
            "Forbidden calls found:" + Environment.NewLine + string.Join(Environment.NewLine, offences));
    }
}