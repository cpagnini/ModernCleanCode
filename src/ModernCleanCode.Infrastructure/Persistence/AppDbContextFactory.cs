using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ModernCleanCode.Infrastructure.Persistence;

/// <summary>Usata solo da `dotnet ef` a design-time (migration), non a runtime.</summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=ModernCleanCode_Dev;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
}
