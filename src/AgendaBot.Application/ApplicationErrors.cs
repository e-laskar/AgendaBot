using AgendaBot.Domain;

namespace AgendaBot.Application;

public static class ApplicationErrors
{
    public static Error EventNotFound(int id) =>
        new("event_not_found", $"Événement #{id} introuvable.");

    public static readonly Error DurationWithAllDayEvent =
        new("duration_with_all_day_event",
            "Un événement sur toute la journée n'a pas de durée : indique une heure ou retire la durée.");

    public static readonly Error NotAllowedToEditEvent =
        new("not_allowed_to_edit_event",
            "Seul le créateur de l'événement ou un membre qui peut gérer les événements peut le modifier.");

    public static readonly Error NothingToEdit =
        new("nothing_to_edit", "Indique au moins un champ à modifier : titre, date, description ou durée.");

    public static Error TooManyUpcomingEvents(int maxUpcomingEventsPerCreator) =>
        new("too_many_upcoming_events",
            $"Limite atteinte : {maxUpcomingEventsPerCreator} événements à venir par membre sur ce serveur.");
}
