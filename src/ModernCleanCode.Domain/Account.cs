namespace ModernCleanCode.Domain;

public sealed class Account
{
    public Guid Id { get; }
    public Money Balance { get; private set; }

    private Account(Guid id, Money balance)
    {
        Id = id;
        Balance = balance;
    }

    public static Account Open(Guid id, Money initialBalance) =>
        id == Guid.Empty ? throw new DomainException("Account id is required.") : new Account(id, initialBalance);

    public void TopUp(Money amount) => Balance = Balance.Add(amount);

    public void Charge(Money amount)
    {
        if (amount.Amount > Balance.Amount)
            throw new DomainException("Insufficient funds.");
        Balance = Balance.Subtract(amount);
    }
}
