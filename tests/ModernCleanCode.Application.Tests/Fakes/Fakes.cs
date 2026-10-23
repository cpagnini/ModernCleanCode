using ModernCleanCode.Application.Abstractions;
using ModernCleanCode.Domain;

namespace ModernCleanCode.Application.Tests.Fakes;

public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly Dictionary<Guid, Account> _accounts = new();

    public int Count => _accounts.Count;

    public Task<Account?> FindAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_accounts.GetValueOrDefault(id));

    public Task AddAsync(Account account, CancellationToken ct)
    {
        _accounts[account.Id] = account;
        return Task.CompletedTask;
    }
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

public sealed class FixedTariffProvider(TollTariff tariff) : ITariffProvider
{
    public TollTariff Current { get; } = tariff;
}
