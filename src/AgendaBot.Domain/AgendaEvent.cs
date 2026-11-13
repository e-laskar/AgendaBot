namespace AgendaBot.Domain;

public sealed class AgendaEvent
{
    private static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(1);

    public int Id { get; }

    public GuildId GuildId { get; }
    public ChannelId ChannelId { get; }
    public UserId CreatedBy { get; }
    public EventTitle Title { get; }
    public EventDescription? Description { get; }

    public DateTimeOffset StartsAt { get; }

    public TimeSpan? Duration { get; }
    public bool IsAllDay { get; }
    public bool ReminderSent { get; private set; }

    private AgendaEvent(
        int id, GuildId guildId, ChannelId channelId, UserId createdBy,
        EventTitle title, EventDescription? description,
        DateTimeOffset startsAt, TimeSpan? duration, bool isAllDay, bool reminderSent)
    {
        Id = id;
        GuildId = guildId;
        ChannelId = channelId;
        CreatedBy = createdBy;
        Title = title;
        Description = description;
        StartsAt = startsAt.ToUniversalTime();
        Duration = duration;
        IsAllDay = isAllDay;
        ReminderSent = reminderSent;
    }

    public static Result<AgendaEvent> Create(
        GuildId guildId, ChannelId channelId, UserId createdBy,
        EventTitle title, EventDescription? description,
        DateTimeOffset startsAt, TimeSpan? duration,
        DateTimeOffset now)
    {
        if (startsAt <= now)
            return DomainErrors.StartInPast;
        if (duration is { } d && d < MinDuration)
            return DomainErrors.InvalidDuration;

        return new AgendaEvent(
            id: 0, guildId, channelId, createdBy, title, description,
            startsAt, duration, isAllDay: false, reminderSent: false);
    }

    public static Result<AgendaEvent> CreateAllDay(
        GuildId guildId, ChannelId channelId, UserId createdBy,
        EventTitle title, EventDescription? description,
        DateTimeOffset dayStartsAt, DateTimeOffset dayEndsAt,
        DateTimeOffset now)
    {
        if (dayEndsAt <= dayStartsAt)
            return DomainErrors.InvalidDuration;
        if (dayEndsAt <= now)
            return DomainErrors.DayInPast;

        return new AgendaEvent(
            id: 0, guildId, channelId, createdBy, title, description,
            dayStartsAt, dayEndsAt - dayStartsAt, isAllDay: true, reminderSent: false);
    }

    public static AgendaEvent RestoreFromPersistence(
        int id, GuildId guildId, ChannelId channelId, UserId createdBy,
        EventTitle title, EventDescription? description,
        DateTimeOffset startsAt, TimeSpan? duration, bool isAllDay, bool reminderSent) =>
        new(id, guildId, channelId, createdBy, title, description, startsAt, duration, isAllDay, reminderSent);

    public bool IsUpcomingAt(DateTimeOffset now) =>
        IsAllDay ? EndOfDay > now : StartsAt >= now;

    private DateTimeOffset EndOfDay => StartsAt + Duration.GetValueOrDefault();

    public bool IsReminderDue(DateTimeOffset now, TimeSpan window) =>
        !ReminderSent && StartsAt > now && StartsAt <= now + window;

    public void MarkReminderSent() => ReminderSent = true;
}
