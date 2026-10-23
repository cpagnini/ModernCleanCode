using Microsoft.EntityFrameworkCore;
using ModernCleanCode.Application.Errors;
using ModernCleanCode.Domain;
using ModernCleanCode.Infrastructure.Persistence;

namespace ModernCleanCode.Infrastructure.Tests;

public class UniqueIdTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Second_save_with_same_id_throws_conflict_exception()
    {
        // Simula due richieste che hanno passato entrambe il controllo "esiste gia'?"
        // dell'handler: controllo e inserimento non sono atomici.
        var id = Guid.NewGuid();
        await using var firstDb = fixture.CreateContext();
        await using var secondDb = fixture.CreateContext();
        await new EfAccountRepository(firstDb).AddAsync(Account.Open(id, Money.Eur(10m)), Ct);
        await new EfAccountRepository(secondDb).AddAsync(Account.Open(id, Money.Eur(99m)), Ct);

        await new EfUnitOfWork(firstDb).SaveChangesAsync(Ct);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new EfUnitOfWork(secondDb).SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task Other_database_errors_are_not_translated_to_conflict()
    {
        // Un trigger di test lancia l'errore SQL 50000 solo per il saldo 666,6666:
        // e' un SqlException che NON e' una violazione di unicita'.
        await using var db = fixture.CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "CREATE OR ALTER TRIGGER TestBoom ON Accounts AFTER INSERT AS " +
            "IF EXISTS (SELECT 1 FROM inserted WHERE Balance = 666.6666) THROW 50000, 'boom', 1;", Ct);
        await new EfAccountRepository(db).AddAsync(Account.Open(Guid.NewGuid(), Money.Eur(666.6666m)), Ct);

        var ex = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() =>
            new EfUnitOfWork(db).SaveChangesAsync(Ct));
        Assert.Equal(50000, Assert.IsType<Microsoft.Data.SqlClient.SqlException>(ex.InnerException).Number);
    }
}
