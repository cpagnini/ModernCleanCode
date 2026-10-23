namespace ModernCleanCode.Domain;

public enum GateDirection { Entry, Exit }

public sealed record GatePassage
{
    public Guid PassageId { get; }
    public string GateId { get; }
    public string TransponderId { get; }
    public DateTimeOffset OccurredAt { get; }
    public GateDirection Direction { get; }

    private GatePassage(Guid passageId, string gateId, string transponderId, DateTimeOffset occurredAt, GateDirection direction)
    {
        PassageId = passageId;
        GateId = gateId;
        TransponderId = transponderId;
        OccurredAt = occurredAt;
        Direction = direction;
    }

    public static GatePassage Create(Guid passageId, string gateId, string transponderId, DateTimeOffset occurredAt, GateDirection direction)
    {
        if (passageId == Guid.Empty) throw new DomainException("PassageId is required.");
        if (string.IsNullOrWhiteSpace(gateId)) throw new DomainException("GateId is required.");
        if (string.IsNullOrWhiteSpace(transponderId)) throw new DomainException("TransponderId is required.");
        return new GatePassage(passageId, gateId, transponderId, occurredAt.ToUniversalTime(), direction);
    }
}
