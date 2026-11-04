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
    public bool ReminderSent { get; private set; }

    private AgendaEvent(
        int id, GuildId guildId, ChannelId channelId, UserId createdBy,
        EventTitle title, EventDescription? description,
        DateTimeOffset startsAt, TimeSpan? duration, bool reminderSent)
    {
        Id = id;
        GuildId = guildId;
        ChannelId = channelId;
        CreatedBy = createdBy;
        Title = title;
        Description = description;
        StartsAt = startsAt.ToUniversalTime();
        Duration = duration;
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
            startsAt, duration, reminderSent: false);
    }

    public static AgendaEvent RestoreFromPersistence(
        int id, GuildId guildId, ChannelId channelId, UserId createdBy,
        EventTitle title, EventDescription? description,
        DateTimeOffset startsAt, TimeSpan? duration, bool reminderSent) =>
        new(id, guildId, channelId, createdBy, title, description, startsAt, duration, reminderSent);

    public bool IsReminderDue(DateTimeOffset now, TimeSpan window) =>
        !ReminderSent && StartsAt > now && StartsAt <= now + window;

    public void MarkReminderSent() => ReminderSent = true;
}
