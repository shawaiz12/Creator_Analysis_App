using System;
using System.Collections.Generic;
using System.Text;

namespace CreatorAnalytics.ArchitectureTests
{
    public class RepoLocatorTests
    {
        [Fact]
        public void Finds_the_solution_folder()
        {
            var root = RepoLocator.FindRoot();
            Assert.True(Directory.Exists(Path.Combine(root, "src")));
        }
    }
}
