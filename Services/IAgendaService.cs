using AgendaBot.Models;

namespace AgendaBot.Services;

public interface IAgendaService
{
    Task<AgendaEvent> AddEventAsync(AgendaEvent agendaEvent);
    Task<IReadOnlyList<AgendaEvent>> GetUpcomingEventsAsync(ulong guildId, int max = 10);
    Task<AgendaEvent?> GetEventAsync(ulong guildId, int id);
    Task<bool> DeleteEventAsync(ulong guildId, int id);
    Task<IReadOnlyList<AgendaEvent>> GetEventsToRemindAsync(DateTime untilUtc);
    Task MarkReminderSentAsync(int id);
}
