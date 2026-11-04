namespace AgendaBot.Domain;

/// <summary>Erreurs métier du domaine. Les messages sont destinés aux utilisateurs Discord.</summary>
public static class DomainErrors
{
    public static readonly Error TitleRequired =
        new("title_required", "Le titre est obligatoire.");

    public static readonly Error TitleTooLong =
        new("title_too_long", $"Le titre ne doit pas dépasser {EventTitle.MaxLength} caractères.");
}
