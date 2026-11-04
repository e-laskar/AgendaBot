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

public sealed record CreationQuota(int MaxUpcomingEventsPerCreator);

public sealed class AddEvent(IEventRepository repository, TimeProvider time, CreationQuota quota)
{
    public async Task<Result<AgendaEvent>> HandleAsync(
        AddEventCommand command, CancellationToken cancellationToken = default)
    {
        var title = EventTitle.Create(command.Title);
        if (!title.IsSuccess) return title.Error;

        var description = EventDescription.CreateOptional(command.Description);
        if (!description.IsSuccess) return description.Error;

        var now = time.GetUtcNow();
        var evt = AgendaEvent.Create(
            command.GuildId, command.ChannelId, command.CreatedBy,
            title.Value, description.Value,
            command.StartsAt, command.Duration,
            now);
        if (!evt.IsSuccess) return evt.Error;

        if (await HasReachedQuotaAsync(command.GuildId, command.CreatedBy, now, cancellationToken))
            return ApplicationErrors.TooManyUpcomingEvents(quota.MaxUpcomingEventsPerCreator);

        return await repository.AddAsync(evt.Value, cancellationToken);
    }

    private async Task<bool> HasReachedQuotaAsync(
        GuildId guildId, UserId creator, DateTimeOffset now, CancellationToken cancellationToken) =>
        await repository.CountUpcomingCreatedByAsync(guildId, creator, now, cancellationToken)
        >= quota.MaxUpcomingEventsPerCreator;
}
