namespace ModernCleanCode.Domain;

public readonly record struct Money
{
    public const string Currency = "EUR";

    public decimal Amount { get; }

    private Money(decimal amount) => Amount = amount;

    public static Money Zero => new(0m);

    public static Money Eur(decimal amount) =>
        amount < 0 ? throw new DomainException("Money cannot be negative.") : new Money(amount);

    public Money Add(Money other) => new(Amount + other.Amount);

    public Money Subtract(Money other) =>
        other.Amount > Amount ? throw new DomainException("Insufficient amount.") : new Money(Amount - other.Amount);
}
