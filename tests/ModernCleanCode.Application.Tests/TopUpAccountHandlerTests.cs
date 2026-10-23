using ModernCleanCode.Application.Accounts;
using ModernCleanCode.Application.Errors;
using ModernCleanCode.Application.Tests.Fakes;
using ModernCleanCode.Domain;

namespace ModernCleanCode.Application.Tests;

public class TopUpAccountHandlerTests
{
    private readonly InMemoryAccountRepository _repo = new();
    private readonly FakeUnitOfWork _uow = new();

    private async Task<Guid> SeedAsync(decimal balance)
    {
        var id = Guid.NewGuid();
        await _repo.AddAsync(Account.Open(id, Money.Eur(balance)), CancellationToken.None);
        return id;
    }

    [Fact]
    public async Task Tops_up_and_saves_once()
    {
        var id = await SeedAsync(10m);
        var dto = await new TopUpAccountHandler(_repo, _uow)
            .HandleAsync(new TopUpAccount(id, 5m), CancellationToken.None);
        Assert.Equal(15m, dto.Balance);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Unknown_account_throws_not_found_without_saving()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new TopUpAccountHandler(_repo, _uow)
                .HandleAsync(new TopUpAccount(Guid.NewGuid(), 5m), CancellationToken.None));
        Assert.Equal(0, _uow.SaveCount);
    }

    [Fact]
    public async Task Negative_amount_throws_domain_exception_without_saving()
    {
        var id = await SeedAsync(10m);
        await Assert.ThrowsAsync<DomainException>(() =>
            new TopUpAccountHandler(_repo, _uow)
                .HandleAsync(new TopUpAccount(id, -5m), CancellationToken.None));
        Assert.Equal(0, _uow.SaveCount);
    }
}
