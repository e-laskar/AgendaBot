using System.Diagnostics;
using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Application.UseCases;

public sealed record AddEventCommand(
    GuildId GuildId,
    ChannelId ChannelId,
    UserId CreatedBy,
    string Title,
    string? Description,
    EventDate Date,
    TimeSpan? Duration);

public abstract record EventDate
{
    private EventDate() { }

    public sealed record AtTime(DateTimeOffset StartsAt) : EventDate;

    public sealed record AllDay(DateTimeOffset DayStartsAt, DateTimeOffset DayEndsAt) : EventDate;
}

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
        var evt = CreateEvent(command, title.Value, description.Value, now);
        if (!evt.IsSuccess) return evt.Error;

        if (await HasReachedQuotaAsync(command.GuildId, command.CreatedBy, now, cancellationToken))
            return ApplicationErrors.TooManyUpcomingEvents(quota.MaxUpcomingEventsPerCreator);

        return await repository.AddAsync(evt.Value, cancellationToken);
    }

    private static Result<AgendaEvent> CreateEvent(
        AddEventCommand command, EventTitle title, EventDescription? description, DateTimeOffset now) =>
        command.Date switch
        {
            EventDate.AtTime atTime => AgendaEvent.Create(
                command.GuildId, command.ChannelId, command.CreatedBy, title, description,
                atTime.StartsAt, command.Duration, now),
            EventDate.AllDay when command.Duration is not null => ApplicationErrors.DurationWithAllDayEvent,
            EventDate.AllDay allDay => AgendaEvent.CreateAllDay(
                command.GuildId, command.ChannelId, command.CreatedBy, title, description,
                allDay.DayStartsAt, allDay.DayEndsAt, now),
            _ => throw new UnreachableException($"Type de date inconnu : {command.Date.GetType().Name}.")
        };

    private async Task<bool> HasReachedQuotaAsync(
        GuildId guildId, UserId creator, DateTimeOffset now, CancellationToken cancellationToken) =>
        await repository.CountUpcomingCreatedByAsync(guildId, creator, now, cancellationToken)
        >= quota.MaxUpcomingEventsPerCreator;
}
