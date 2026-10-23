using ModernCleanCode.Application.Accounts;
using ModernCleanCode.Application.Errors;
using ModernCleanCode.Application.Tests.Fakes;
using ModernCleanCode.Domain;

namespace ModernCleanCode.Application.Tests;

public class ChargeTollHandlerTests
{
    private readonly InMemoryAccountRepository _repo = new();
    private readonly FakeUnitOfWork _uow = new();

    private ChargeTollHandler Handler =>
        new(_repo, new FixedTariffProvider(new TollTariff(Money.Eur(0.07m))), _uow);

    private async Task<Guid> SeedAsync(decimal balance)
    {
        var id = Guid.NewGuid();
        await _repo.AddAsync(Account.Open(id, Money.Eur(balance)), CancellationToken.None);
        return id;
    }

    [Fact]
    public async Task Charges_tariff_for_distance_and_saves_once()
    {
        var id = await SeedAsync(10m);
        var result = await Handler.HandleAsync(new ChargeToll(id, 10, 30), CancellationToken.None);
        Assert.Equal(new ChargeTollResult(1.40m, 8.60m), result);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Unknown_account_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler.HandleAsync(new ChargeToll(Guid.NewGuid(), 0, 10), CancellationToken.None));
        Assert.Equal(0, _uow.SaveCount);
    }

    [Fact]
    public async Task Insufficient_funds_throws_domain_exception_and_does_not_save()
    {
        var id = await SeedAsync(1m);
        await Assert.ThrowsAsync<DomainException>(() =>
            Handler.HandleAsync(new ChargeToll(id, 0, 100), CancellationToken.None));
        Assert.Equal(0, _uow.SaveCount);
        var stored = await _repo.FindAsync(id, CancellationToken.None);
        Assert.Equal(1m, stored!.Balance.Amount);
    }

    [Fact]
    public async Task Invalid_km_range_throws_domain_exception_without_saving()
    {
        var id = await SeedAsync(10m);
        await Assert.ThrowsAsync<DomainException>(() =>
            Handler.HandleAsync(new ChargeToll(id, 20, 20), CancellationToken.None));
        Assert.Equal(0, _uow.SaveCount);
    }
}
