using ModernCleanCode.Application.Accounts;
using ModernCleanCode.Domain;
using ModernCleanCode.Infrastructure.Persistence;

namespace ModernCleanCode.Infrastructure.Tests;

public class AccountRepositoryTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private async Task SaveNewAsync(Account account)
    {
        await using var db = fixture.CreateContext();
        await new EfAccountRepository(db).AddAsync(account, Ct);
        await new EfUnitOfWork(db).SaveChangesAsync(Ct);
    }

    private async Task<Account?> ReloadAsync(Guid id)
    {
        await using var db = fixture.CreateContext();
        return await new EfAccountRepository(db).FindAsync(id, Ct);
    }

    [Fact]
    public async Task Add_and_save_then_find_in_new_context_returns_same_balance()
    {
        var id = Guid.NewGuid();
        await SaveNewAsync(Account.Open(id, Money.Eur(12.50m)));

        var loaded = await ReloadAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal(Money.Eur(12.50m), loaded.Balance);
    }

    [Fact]
    public async Task Balance_keeps_four_decimals()
    {
        var id = Guid.NewGuid();
        await SaveNewAsync(Account.Open(id, Money.Eur(10.0333m)));

        Assert.Equal(Money.Eur(10.0333m), (await ReloadAsync(id))!.Balance);
    }

    [Fact]
    public async Task Find_unknown_id_returns_null() =>
        Assert.Null(await ReloadAsync(Guid.NewGuid()));

    [Fact]
    public async Task Add_without_save_is_not_persisted()
    {
        var id = Guid.NewGuid();
        await using (var db = fixture.CreateContext())
            await new EfAccountRepository(db).AddAsync(Account.Open(id, Money.Eur(5m)), Ct);

        Assert.Null(await ReloadAsync(id));
    }

    [Fact]
    public async Task Charge_toll_end_to_end_persists_new_balance()
    {
        var id = Guid.NewGuid();
        await SaveNewAsync(Account.Open(id, Money.Eur(10m)));

        await using (var db = fixture.CreateContext())
        {
            var handler = new ChargeTollHandler(
                new EfAccountRepository(db),
                new TestTariffProvider(new TollTariff(Money.Eur(0.07m))),
                new EfUnitOfWork(db));
            await handler.HandleAsync(new ChargeToll(id, 10, 30), Ct);
        }

        Assert.Equal(Money.Eur(8.60m), (await ReloadAsync(id))!.Balance);
    }

    private sealed class TestTariffProvider(TollTariff tariff) : Application.Abstractions.ITariffProvider
    {
        public TollTariff Current { get; } = tariff;
    }
}
