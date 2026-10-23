using Microsoft.EntityFrameworkCore;
using ModernCleanCode.Infrastructure.Persistence;

namespace ModernCleanCode.Infrastructure.Tests;

/// <summary>
/// Un database LocalDB nuovo per ogni classe di test, eliminato alla fine.
/// Il cleanup tocca solo database con il prefisso di test.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string Prefix = "ModernCleanCode_Test_";
    private readonly string _databaseName = Prefix + Guid.NewGuid().ToString("N");

    private string ConnectionString =>
        $@"Server=(localdb)\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!_databaseName.StartsWith(Prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to drop a database without the test prefix.");
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}
