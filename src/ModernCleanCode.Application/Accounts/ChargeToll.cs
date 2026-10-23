using ModernCleanCode.Application.Abstractions;
using ModernCleanCode.Application.Errors;

namespace ModernCleanCode.Application.Accounts;

public sealed record ChargeToll(Guid AccountId, int EntryKm, int ExitKm);

public sealed record ChargeTollResult(decimal Charged, decimal Balance);

public sealed class ChargeTollHandler(IAccountRepository accounts, ITariffProvider tariffs, IUnitOfWork unitOfWork)
    : ICommandHandler<ChargeToll, ChargeTollResult>
{
    public async Task<ChargeTollResult> HandleAsync(ChargeToll command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var account = await accounts.FindAsync(command.AccountId, ct)
            ?? throw new NotFoundException($"Account {command.AccountId} not found.");

        var toll = tariffs.Current.Calculate(command.EntryKm, command.ExitKm);
        account.Charge(toll);
        await unitOfWork.SaveChangesAsync(ct);
        return new ChargeTollResult(toll.Amount, account.Balance.Amount);
    }
}
