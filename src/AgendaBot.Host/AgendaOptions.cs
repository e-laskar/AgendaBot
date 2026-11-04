namespace AgendaBot;

internal sealed class AgendaOptions
{
    public const string SectionName = "Agenda";

    public int MaxUpcomingEventsPerCreator { get; set; } = 10;
}
