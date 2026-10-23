using ModernCleanCode.Domain;

namespace ModernCleanCode.Domain.Tests;

public class GatePassageTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 10, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void Create_with_empty_passage_id_throws() =>
        Assert.Throws<DomainException>(() =>
            GatePassage.Create(Guid.Empty, "G1", "T1", Now, GateDirection.Entry));

    [Fact]
    public void Create_with_blank_gate_throws() =>
        Assert.Throws<DomainException>(() =>
            GatePassage.Create(Guid.NewGuid(), "  ", "T1", Now, GateDirection.Entry));

    [Fact]
    public void Create_with_blank_transponder_throws() =>
        Assert.Throws<DomainException>(() =>
            GatePassage.Create(Guid.NewGuid(), "G1", "", Now, GateDirection.Entry));

    [Fact]
    public void Create_normalizes_time_to_utc()
    {
        var passage = GatePassage.Create(Guid.NewGuid(), "G1", "T1", Now, GateDirection.Exit);
        Assert.Equal(TimeSpan.Zero, passage.OccurredAt.Offset);
        Assert.Equal(Now.UtcDateTime, passage.OccurredAt.UtcDateTime);
    }
}
