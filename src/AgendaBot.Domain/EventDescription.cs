namespace AgendaBot.Domain;

/// <summary>Description optionnelle d'un événement : <see cref="MaxLength"/> caractères au plus.</summary>
public sealed record EventDescription
{
    public const int MaxLength = 1000;

    public string Value { get; }

    private EventDescription(string value) => Value = value;

    /// <summary>Une valeur absente ou vide donne un succès sans description (<c>null</c>).</summary>
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
