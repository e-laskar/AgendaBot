namespace AgendaBot.Domain;

public static class DomainErrors
{
    public static readonly Error TitleRequired =
        new("title_required", "Le titre est obligatoire.");

    public static readonly Error TitleTooLong =
        new("title_too_long", $"Le titre ne doit pas dépasser {EventTitle.MaxLength} caractères.");

    public static readonly Error DescriptionTooLong =
        new("description_too_long", $"La description ne doit pas dépasser {EventDescription.MaxLength} caractères.");

    public static readonly Error StartInPast =
        new("start_in_past", "La date doit être dans le futur.");

    public static readonly Error DayInPast =
        new("day_in_past", "Ce jour est déjà passé.");

    public static readonly Error EventAlreadyPast =
        new("event_already_past", "Cet événement est déjà passé : il ne peut plus être modifié.");

    public static readonly Error InvalidDuration =
        new("invalid_duration", "La durée doit être d'au moins une minute.");
}
