using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Application.UseCases;

public sealed record AddEventCommand(
    GuildId GuildId,
    ChannelId ChannelId,
    UserId CreatedBy,
    string Title,
    string? Description,
    DateTimeOffset StartsAt,
    TimeSpan? Duration);

public sealed class AddEvent(IEventRepository repository, TimeProvider time)
{
    public async Task<Result<AgendaEvent>> HandleAsync(
        AddEventCommand command, CancellationToken cancellationToken = default)
    {
        var title = EventTitle.Create(command.Title);
        if (!title.IsSuccess) return title.Error;

        var description = EventDescription.CreateOptional(command.Description);
        if (!description.IsSuccess) return description.Error;

        var evt = AgendaEvent.Create(
            command.GuildId, command.ChannelId, command.CreatedBy,
            title.Value, description.Value,
            command.StartsAt, command.Duration,
            time.GetUtcNow());
        if (!evt.IsSuccess) return evt.Error;

        return await repository.AddAsync(evt.Value, cancellationToken);
    }
}
