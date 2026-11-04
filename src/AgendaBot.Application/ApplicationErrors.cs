using AgendaBot.Domain;

namespace AgendaBot.Application;

/// <summary>Erreurs des cas d'usage. Les messages sont destinés aux utilisateurs Discord.</summary>
public static class ApplicationErrors
{
    public static Error EventNotFound(int id) =>
        new("event_not_found", $"Événement #{id} introuvable.");
}
