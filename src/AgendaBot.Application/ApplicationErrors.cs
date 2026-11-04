using AgendaBot.Domain;

namespace AgendaBot.Application;

public static class ApplicationErrors
{
    public static Error EventNotFound(int id) =>
        new("event_not_found", $"Événement #{id} introuvable.");
}
