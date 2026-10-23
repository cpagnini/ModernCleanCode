using Microsoft.EntityFrameworkCore;

namespace ModernCleanCode.Infrastructure.Tests;

public class ModelTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Model_has_no_pending_changes_compared_to_migrations()
    {
        await using var db = fixture.CreateContext();
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
