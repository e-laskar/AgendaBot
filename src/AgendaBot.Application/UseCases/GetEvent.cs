using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Application.UseCases;

public sealed class GetEvent(IEventRepository repository)
{
    public async Task<Result<AgendaEvent>> HandleAsync(
        GuildId guildId, int id, CancellationToken cancellationToken = default)
    {
        var evt = await repository.GetAsync(guildId, id, cancellationToken);
        return evt is null ? ApplicationErrors.EventNotFound(id) : evt;
    }
}
