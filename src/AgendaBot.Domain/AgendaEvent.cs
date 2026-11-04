namespace AgendaBot.Domain;

/// <summary>Événement de l'agenda d'un serveur Discord.</summary>
public sealed class AgendaEvent
{
    private static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(1);

    /// <summary>Attribué par la persistance ; 0 tant que l'événement n'est pas enregistré.</summary>
    public int Id { get; private set; }

    public GuildId GuildId { get; }
    public ChannelId ChannelId { get; }
    public UserId CreatedBy { get; }
    public EventTitle Title { get; }
    public EventDescription? Description { get; }

    /// <summary>Début de l'événement, toujours en UTC.</summary>
    public DateTimeOffset StartsAt { get; }

    public TimeSpan? Duration { get; }
    public bool ReminderSent { get; private set; }

    private AgendaEvent(
        GuildId guildId, ChannelId channelId, UserId createdBy,
        EventTitle title, EventDescription? description,
        DateTimeOffset startsAt, TimeSpan? duration)
    {
        GuildId = guildId;
        ChannelId = channelId;
        CreatedBy = createdBy;
        Title = title;
        Description = description;
        StartsAt = startsAt;
        Duration = duration;
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
            guildId, channelId, createdBy, title, description,
            startsAt.ToUniversalTime(), duration);
    }
}
