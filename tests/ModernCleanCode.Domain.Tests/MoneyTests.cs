using ModernCleanCode.Domain;

namespace ModernCleanCode.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Eur_negative_amount_throws() =>
        Assert.Throws<DomainException>(() => Money.Eur(-0.01m));

    [Fact]
    public void Add_sums_amounts() =>
        Assert.Equal(Money.Eur(3.30m), Money.Eur(1.10m).Add(Money.Eur(2.20m)));

    [Fact]
    public void Subtract_more_than_available_throws() =>
        Assert.Throws<DomainException>(() => Money.Eur(1m).Subtract(Money.Eur(1.01m)));

    [Fact]
    public void Subtract_exact_gives_zero() =>
        Assert.Equal(Money.Zero, Money.Eur(5m).Subtract(Money.Eur(5m)));
}
