using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Application.UseCases;

public sealed class ListUpcomingEvents(IEventRepository repository, TimeProvider time)
{
    public Task<IReadOnlyList<AgendaEvent>> HandleAsync(
        GuildId guildId, int max, CancellationToken cancellationToken = default) =>
        repository.GetUpcomingByStartDateAsync(guildId, time.GetUtcNow(), max, cancellationToken);
}
