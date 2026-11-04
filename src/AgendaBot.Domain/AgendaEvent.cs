namespace AgendaBot.Domain;

/// <summary>Événement de l'agenda d'un serveur Discord.</summary>
public sealed class AgendaEvent
{
    private static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(1);

    /// <summary>Attribué par la persistance ; 0 tant que l'événement n'est pas enregistré.</summary>
    public int Id { get; }

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

    /// <summary>
    /// Recharge un événement déjà enregistré, tel qu'il a été persisté.
    /// Les règles de création ne s'appliquent pas (la date peut être passée) : réservé à la persistance.
    /// </summary>
    public static AgendaEvent Restore(
        int id, GuildId guildId, ChannelId channelId, UserId createdBy,
        EventTitle title, EventDescription? description,
        DateTimeOffset startsAt, TimeSpan? duration, bool reminderSent) =>
        new(id, guildId, channelId, createdBy, title, description, startsAt, duration, reminderSent);

    /// <summary>
    /// Vrai si le rappel n'a pas encore été envoyé et que l'événement, pas encore commencé,
    /// commence au plus tard à <paramref name="now"/> + <paramref name="window"/>.
    /// </summary>
    public bool IsReminderDue(DateTimeOffset now, TimeSpan window) =>
        !ReminderSent && StartsAt > now && StartsAt <= now + window;

    /// <summary>Idempotent : peut être appelé plusieurs fois sans effet supplémentaire.</summary>
    public void MarkReminderSent() => ReminderSent = true;
}
