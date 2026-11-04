using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Application.UseCases;

/// <summary>
/// Supprime un événement d'un serveur et le renvoie. Le contrôle des permissions
/// (« Gérer les événements ») est fait par l'adaptateur Discord.
/// </summary>
public sealed class DeleteEvent(IEventRepository repository)
{
    public async Task<Result<AgendaEvent>> HandleAsync(
        GuildId guildId, int id, CancellationToken cancellationToken = default)
    {
        var evt = await repository.GetAsync(guildId, id, cancellationToken);
        if (evt is null)
            return ApplicationErrors.EventNotFound(id);

        await repository.DeleteAsync(evt, cancellationToken);
        return evt;
    }
}
