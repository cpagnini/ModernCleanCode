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
}
