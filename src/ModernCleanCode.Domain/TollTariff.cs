namespace ModernCleanCode.Domain;

public sealed record TollTariff(Money RatePerKm)
{
    public Money Calculate(int entryKm, int exitKm)
    {
        if (exitKm <= entryKm)
            throw new DomainException("Exit must be after entry.");
        var raw = RatePerKm.Amount * (exitKm - entryKm);
        return Money.Eur(Math.Round(raw, 2, MidpointRounding.AwayFromZero));
    }
}
