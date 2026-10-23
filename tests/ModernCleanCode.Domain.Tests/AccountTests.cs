using ModernCleanCode.Domain;

namespace ModernCleanCode.Domain.Tests;

public class AccountTests
{
    private static Account OpenWith(decimal euro) => Account.Open(Guid.NewGuid(), Money.Eur(euro));

    [Fact]
    public void Charge_equal_to_balance_leaves_zero()
    {
        var account = OpenWith(10m);
        account.Charge(Money.Eur(10m));
        Assert.Equal(Money.Zero, account.Balance);
    }

    [Fact]
    public void Charge_above_balance_throws_and_keeps_balance()
    {
        var account = OpenWith(10m);
        Assert.Throws<DomainException>(() => account.Charge(Money.Eur(10.01m)));
        Assert.Equal(Money.Eur(10m), account.Balance);
    }

    [Fact]
    public void TopUp_increases_balance()
    {
        var account = OpenWith(10m);
        account.TopUp(Money.Eur(5m));
        Assert.Equal(Money.Eur(15m), account.Balance);
    }
}
