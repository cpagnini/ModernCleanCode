using ModernCleanCode.Infrastructure.Persistence;

namespace ModernCleanCode.Infrastructure.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Infrastructure_references_only_Application_and_Domain_among_project_assemblies()
    {
        var referenced = typeof(AppDbContext).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("ModernCleanCode", StringComparison.Ordinal))
            .Distinct()
            .Order();

        Assert.Equal(["ModernCleanCode.Application", "ModernCleanCode.Domain"], referenced);
    }
}
