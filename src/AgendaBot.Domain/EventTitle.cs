namespace AgendaBot.Domain;

public sealed record EventTitle
{
    public const int MaxLength = 100;

    public string Value { get; }

    private EventTitle(string value) => Value = value;

    public static Result<EventTitle> Create(string? value)
    {
        var trimmed = value?.Trim() ?? "";

        if (trimmed.Length == 0)
            return DomainErrors.TitleRequired;
        if (trimmed.Length > MaxLength)
            return DomainErrors.TitleTooLong;

        return new EventTitle(trimmed);
    }

    public override string ToString() => Value;
}
