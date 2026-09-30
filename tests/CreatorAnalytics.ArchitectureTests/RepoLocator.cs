using System;
using System.Collections.Generic;
using System.Text;

namespace CreatorAnalytics.ArchitectureTests;

public static class RepoLocator
{
    public static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0 || dir.GetFiles("*.slnx").Length > 0)
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Solution folder not found.");
    }
}
