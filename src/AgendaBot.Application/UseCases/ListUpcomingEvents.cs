using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Application.UseCases;

/// <summary>Liste les prochains événements d'un serveur, du plus proche au plus lointain.</summary>
public sealed class ListUpcomingEvents(IEventRepository repository, TimeProvider time)
{
    public Task<IReadOnlyList<AgendaEvent>> HandleAsync(
        GuildId guildId, int max, CancellationToken cancellationToken = default) =>
        repository.GetUpcomingAsync(guildId, time.GetUtcNow(), max, cancellationToken);
}
