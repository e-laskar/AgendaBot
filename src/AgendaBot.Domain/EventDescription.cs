namespace AgendaBot.Domain;

public sealed record EventDescription
{
    public const int MaxLength = 1000;

    public string Value { get; }

    private EventDescription(string value) => Value = value;

    public static Result<EventDescription?> CreateOptional(string? value)
    {
        var trimmed = value?.Trim() ?? "";

        if (trimmed.Length == 0)
            return (EventDescription?)null;
        if (trimmed.Length > MaxLength)
            return DomainErrors.DescriptionTooLong;

        return new EventDescription(trimmed);
    }

    public override string ToString() => Value;
}
