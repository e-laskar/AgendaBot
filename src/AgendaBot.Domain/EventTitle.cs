namespace AgendaBot.Domain;

/// <summary>Titre d'un événement : non vide, sans espaces superflus, <see cref="MaxLength"/> caractères au plus.</summary>
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
