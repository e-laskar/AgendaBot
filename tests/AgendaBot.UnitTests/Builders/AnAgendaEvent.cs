using AgendaBot.Domain;

namespace AgendaBot.UnitTests.Builders;

internal sealed class AnAgendaEvent
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
    public static readonly GuildId DefaultGuild = new(1);

    private GuildId _guildId = DefaultGuild;
    private ChannelId _channelId = new(2);
    private UserId _createdBy = new(3);
    private string _title = "Réunion";
    private string? _description;
    private DateTimeOffset _startsAt = DefaultNow.AddDays(1);
    private TimeSpan? _duration;
    private DateTimeOffset _now = DefaultNow;

    public AnAgendaEvent InGuild(GuildId guildId) { _guildId = guildId; return this; }
    public AnAgendaEvent InChannel(ChannelId channelId) { _channelId = channelId; return this; }
    public AnAgendaEvent CreatedBy(UserId userId) { _createdBy = userId; return this; }
    public AnAgendaEvent Titled(string title) { _title = title; return this; }
    public AnAgendaEvent DescribedAs(string? description) { _description = description; return this; }
    public AnAgendaEvent StartingAt(DateTimeOffset startsAt) { _startsAt = startsAt; return this; }
    public AnAgendaEvent Lasting(TimeSpan? duration) { _duration = duration; return this; }
    public AnAgendaEvent CreatedAt(DateTimeOffset now) { _now = now; return this; }

    public Result<AgendaEvent> TryBuild() =>
        AgendaEvent.Create(
            _guildId, _channelId, _createdBy,
            EventTitle.Create(_title).Value,
            EventDescription.CreateOptional(_description).Value,
            _startsAt, _duration, _now);

    public AgendaEvent Build() => TryBuild().Value;

    public AgendaEvent BuildPersisted(int id, bool reminderSent = false) =>
        AgendaEvent.RestoreFromPersistence(
            id, _guildId, _channelId, _createdBy,
            EventTitle.Create(_title).Value,
            EventDescription.CreateOptional(_description).Value,
            _startsAt, _duration, reminderSent);
}
