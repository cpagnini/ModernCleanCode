using ModernCleanCode.Application.Accounts;
using ModernCleanCode.Application.Errors;
using ModernCleanCode.Application.Tests.Fakes;
using ModernCleanCode.Domain;

namespace ModernCleanCode.Application.Tests;

public class OpenAccountHandlerTests
{
    private readonly InMemoryAccountRepository _repo = new();
    private readonly FakeUnitOfWork _uow = new();
    private OpenAccountHandler Handler => new(_repo, _uow);

    [Fact]
    public async Task Opens_account_and_saves_once()
    {
        var id = Guid.NewGuid();
        var dto = await Handler.HandleAsync(new OpenAccount(id, 25m), CancellationToken.None);
        Assert.Equal(new AccountDto(id, 25m), dto);
        Assert.Equal(1, _uow.SaveCount);
        Assert.Equal(1, _repo.Count);
    }

    [Fact]
    public async Task Existing_id_throws_conflict_and_keeps_account()
    {
        var id = Guid.NewGuid();
        await Handler.HandleAsync(new OpenAccount(id, 25m), CancellationToken.None);
        await Assert.ThrowsAsync<ConflictException>(() =>
            Handler.HandleAsync(new OpenAccount(id, 99m), CancellationToken.None));
        var stored = await _repo.FindAsync(id, CancellationToken.None);
        Assert.Equal(25m, stored!.Balance.Amount);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Negative_initial_balance_throws_domain_exception_without_saving()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            Handler.HandleAsync(new OpenAccount(Guid.NewGuid(), -1m), CancellationToken.None));
        Assert.Equal(0, _uow.SaveCount);
    }

    [Fact]
    public async Task Cancelled_token_throws_before_saving()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Handler.HandleAsync(new OpenAccount(Guid.NewGuid(), 1m), cts.Token));
        Assert.Equal(0, _uow.SaveCount);
    }
}
