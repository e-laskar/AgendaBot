using AgendaBot.Domain;

namespace AgendaBot.Application.Ports;

public interface IEventRepository
{
    Task<AgendaEvent> AddAsync(AgendaEvent evt, CancellationToken cancellationToken = default);

    Task<AgendaEvent?> GetAsync(GuildId guildId, int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgendaEvent>> GetUpcomingByStartDateAsync(
        GuildId guildId, DateTimeOffset from, int max, CancellationToken cancellationToken = default);

    Task<int> CountUpcomingCreatedByAsync(
        GuildId guildId, UserId creator, DateTimeOffset from, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgendaEvent>> GetWithoutReminderStartingBetweenAsync(
        DateTimeOffset after, DateTimeOffset until, CancellationToken cancellationToken = default);

    Task<bool> TryClaimReminderAsync(AgendaEvent evt, CancellationToken cancellationToken = default);

    Task ReleaseReminderClaimAsync(AgendaEvent evt, CancellationToken cancellationToken = default);

    Task DeleteAsync(AgendaEvent evt, CancellationToken cancellationToken = default);
}
