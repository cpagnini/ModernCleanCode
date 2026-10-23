using ModernCleanCode.Application.Accounts;
using ModernCleanCode.Domain;

namespace ModernCleanCode.Application.Tests;

public class ArchitectureTests
{
    private static IEnumerable<string> ProjectReferences(System.Reflection.Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("ModernCleanCode", StringComparison.Ordinal));

    [Fact]
    public void Domain_references_no_project_assembly() =>
        Assert.Empty(ProjectReferences(typeof(Money).Assembly));

    [Fact]
    public void Application_references_only_Domain_among_project_assemblies() =>
        Assert.Equal(["ModernCleanCode.Domain"],
            ProjectReferences(typeof(OpenAccountHandler).Assembly).Distinct());
}
