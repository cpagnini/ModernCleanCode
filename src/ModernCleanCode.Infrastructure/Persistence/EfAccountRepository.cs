using ModernCleanCode.Application.Abstractions;
using ModernCleanCode.Domain;

namespace ModernCleanCode.Infrastructure.Persistence;

public sealed class EfAccountRepository(AppDbContext db) : IAccountRepository
{
    public async Task<Account?> FindAsync(Guid id, CancellationToken ct) =>
        await db.Accounts.FindAsync([id], ct);

    public async Task AddAsync(Account account, CancellationToken ct) =>
        await db.Accounts.AddAsync(account, ct);
}
