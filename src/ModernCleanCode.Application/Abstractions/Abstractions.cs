using ModernCleanCode.Domain;

namespace ModernCleanCode.Application.Abstractions;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IAccountRepository
{
    Task<Account?> FindAsync(Guid id, CancellationToken ct);
    Task AddAsync(Account account, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ITariffProvider
{
    TollTariff Current { get; }
}
