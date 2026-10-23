using ModernCleanCode.Application.Abstractions;
using ModernCleanCode.Application.Errors;
using ModernCleanCode.Domain;

namespace ModernCleanCode.Application.Accounts;

public sealed record OpenAccount(Guid AccountId, decimal InitialBalance);

public sealed class OpenAccountHandler(IAccountRepository accounts, IUnitOfWork unitOfWork)
    : ICommandHandler<OpenAccount, AccountDto>
{
    public async Task<AccountDto> HandleAsync(OpenAccount command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (await accounts.FindAsync(command.AccountId, ct) is not null)
            throw new ConflictException($"Account {command.AccountId} already exists.");

        var account = Account.Open(command.AccountId, Money.Eur(command.InitialBalance));
        await accounts.AddAsync(account, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new AccountDto(account.Id, account.Balance.Amount);
    }
}
