using System.Diagnostics;
using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Application.UseCases;

public sealed record EditEventCommand(
    GuildId GuildId,
    int Id,
    UserId EditedBy,
    bool CanManageEvents,
    string? Title,
    string? Description,
    EventDate? Date,
    TimeSpan? Duration)
{
    public bool ChangesDetails => Title is not null || Description is not null;
    public bool ChangesSchedule => Date is not null || Duration is not null;
}

public sealed class EditEvent(IEventRepository repository, TimeProvider time)
{
    public async Task<Result<AgendaEvent>> HandleAsync(
        EditEventCommand command, CancellationToken cancellationToken = default)
    {
        if (!command.ChangesDetails && !command.ChangesSchedule)
            return ApplicationErrors.NothingToEdit;

        var evt = await repository.GetAsync(command.GuildId, command.Id, cancellationToken);
        if (evt is null)
            return ApplicationErrors.EventNotFound(command.Id);
        if (!IsAllowedToEdit(evt, command))
            return ApplicationErrors.NotAllowedToEditEvent;

        var now = time.GetUtcNow();
        var described = Describe(evt, command, now);
        if (!described.IsSuccess) return described.Error;

        var edited = Reschedule(described.Value, command, now);
        if (!edited.IsSuccess) return edited.Error;

        if (!await repository.UpdateAsync(edited.Value, cancellationToken))
            return ApplicationErrors.EventNotFound(command.Id);

        return edited.Value;
    }

    private static bool IsAllowedToEdit(AgendaEvent evt, EditEventCommand command) =>
        command.CanManageEvents || evt.CreatedBy == command.EditedBy;

    private static Result<AgendaEvent> Describe(AgendaEvent evt, EditEventCommand command, DateTimeOffset now)
    {
        if (!command.ChangesDetails)
            return evt;

        var title = command.Title is null ? evt.Title : EventTitle.Create(command.Title);
        if (!title.IsSuccess) return title.Error;

        var description = command.Description is null ? evt.Description : EventDescription.CreateOptional(command.Description);
        if (!description.IsSuccess) return description.Error;

        return evt.Describe(title.Value, description.Value, now);
    }

    private static Result<AgendaEvent> Reschedule(AgendaEvent evt, EditEventCommand command, DateTimeOffset now)
    {
        if (!command.ChangesSchedule)
            return evt;

        return (command.Date ?? CurrentDateOf(evt)) switch
        {
            EventDate.AllDay when command.Duration is not null => ApplicationErrors.DurationWithAllDayEvent,
            EventDate.AllDay allDay => evt.RescheduleAllDay(allDay.DayStartsAt, allDay.DayEndsAt, now),
            EventDate.AtTime atTime => evt.Reschedule(atTime.StartsAt, command.Duration ?? DurationKeptWhenMoving(evt), now),
            var date => throw new UnreachableException($"Type de date inconnu : {date.GetType().Name}.")
        };
    }

    private static EventDate CurrentDateOf(AgendaEvent evt) =>
        evt.IsAllDay
            ? new EventDate.AllDay(evt.StartsAt, evt.StartsAt + evt.Duration.GetValueOrDefault())
            : new EventDate.AtTime(evt.StartsAt);

    private static TimeSpan? DurationKeptWhenMoving(AgendaEvent evt) => evt.IsAllDay ? null : evt.Duration;
}
