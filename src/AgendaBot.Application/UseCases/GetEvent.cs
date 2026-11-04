using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Application.UseCases;

/// <summary>Récupère un événement d'un serveur. Les événements des autres serveurs sont invisibles.</summary>
public sealed class GetEvent(IEventRepository repository)
{
    public async Task<Result<AgendaEvent>> HandleAsync(
        GuildId guildId, int id, CancellationToken cancellationToken = default)
    {
        var evt = await repository.GetAsync(guildId, id, cancellationToken);
        return evt is null ? ApplicationErrors.EventNotFound(id) : evt;
    }
}
