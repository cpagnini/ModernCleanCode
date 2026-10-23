using ModernCleanCode.Application.Abstractions;
using ModernCleanCode.Application.Errors;
using ModernCleanCode.Domain;

namespace ModernCleanCode.Application.Accounts;

public sealed record TopUpAccount(Guid AccountId, decimal Amount);

public sealed class TopUpAccountHandler(IAccountRepository accounts, IUnitOfWork unitOfWork)
    : ICommandHandler<TopUpAccount, AccountDto>
{
    public async Task<AccountDto> HandleAsync(TopUpAccount command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var account = await accounts.FindAsync(command.AccountId, ct)
            ?? throw new NotFoundException($"Account {command.AccountId} not found.");

        account.TopUp(Money.Eur(command.Amount));
        await unitOfWork.SaveChangesAsync(ct);
        return new AccountDto(account.Id, account.Balance.Amount);
    }
}
