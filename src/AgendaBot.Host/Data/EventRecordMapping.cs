using AgendaBot.Domain;

namespace AgendaBot.Data;

internal static class EventRecordMapping
{
    public static EventRecord ToRecord(this AgendaEvent evt) => new()
    {
        GuildId = evt.GuildId.Value,
        ChannelId = evt.ChannelId.Value,
        CreatedByUserId = evt.CreatedBy.Value,
        Title = evt.Title.Value,
        Description = evt.Description?.Value,
        StartsAtUtc = evt.StartsAt.UtcDateTime,
        DurationMinutes = evt.Duration is { } duration ? (int)duration.TotalMinutes : null,
        ReminderSent = evt.ReminderSent
    };

    public static AgendaEvent ToDomain(this EventRecord record) =>
        AgendaEvent.RestoreFromPersistence(
            record.Id,
            new GuildId(record.GuildId),
            new ChannelId(record.ChannelId),
            new UserId(record.CreatedByUserId),
            EventTitle.Create(record.Title).Value,
            EventDescription.CreateOptional(record.Description).Value,
            new DateTimeOffset(DateTime.SpecifyKind(record.StartsAtUtc, DateTimeKind.Utc)),
            record.DurationMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : null,
            record.ReminderSent);
}
