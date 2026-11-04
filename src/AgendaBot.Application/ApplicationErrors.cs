using AgendaBot.Domain;

namespace AgendaBot.Application;

public static class ApplicationErrors
{
    public static Error EventNotFound(int id) =>
        new("event_not_found", $"Événement #{id} introuvable.");

    public static Error TooManyUpcomingEvents(int maxUpcomingEventsPerCreator) =>
        new("too_many_upcoming_events",
            $"Limite atteinte : {maxUpcomingEventsPerCreator} événements à venir par membre sur ce serveur.");
}
