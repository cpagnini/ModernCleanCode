using ModernCleanCode.Domain;

namespace ModernCleanCode.Domain.Tests;

public class TollTariffTests
{
    [Fact]
    public void Calculate_multiplies_rate_by_distance() =>
        Assert.Equal(Money.Eur(1.40m), new TollTariff(Money.Eur(0.07m)).Calculate(10, 30));

    [Fact]
    public void Calculate_rounds_away_from_zero()
    {
        Assert.Equal(Money.Eur(0.03m), new TollTariff(Money.Eur(0.0333m)).Calculate(0, 1));
        Assert.Equal(Money.Eur(0.04m), new TollTariff(Money.Eur(0.035m)).Calculate(0, 1));
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(30, 10)]
    public void Calculate_exit_not_after_entry_throws(int entryKm, int exitKm) =>
        Assert.Throws<DomainException>(() => new TollTariff(Money.Eur(0.07m)).Calculate(entryKm, exitKm));
}
