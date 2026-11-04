using AgendaBot.Domain;

namespace AgendaBot.UnitTests.Domain;

public class AgendaEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly EventTitle Title = EventTitle.Create("Réunion").Value;

    private static Result<AgendaEvent> CreateEvent(
        DateTimeOffset? startsAt = null,
        TimeSpan? duration = null,
        EventDescription? description = null) =>
        AgendaEvent.Create(
            new GuildId(1), new ChannelId(2), new UserId(3),
            Title, description,
            startsAt ?? Now.AddDays(1), duration,
            Now);

    [Fact]
    public void Creates_event_with_given_values()
    {
        var description = EventDescription.CreateOptional("Salle 2").Value;
        var startsAt = Now.AddDays(1);

        var evt = CreateEvent(startsAt, TimeSpan.FromMinutes(90), description).Value;

        evt.ShouldSatisfyAllConditions(
            () => evt.GuildId.ShouldBe(new GuildId(1)),
            () => evt.ChannelId.ShouldBe(new ChannelId(2)),
            () => evt.CreatedBy.ShouldBe(new UserId(3)),
            () => evt.Title.ShouldBe(Title),
            () => evt.Description.ShouldBe(description),
            () => evt.StartsAt.ShouldBe(startsAt),
            () => evt.Duration.ShouldBe(TimeSpan.FromMinutes(90)));
    }

    [Fact]
    public void New_event_has_no_reminder_sent()
    {
        CreateEvent().Value.ReminderSent.ShouldBeFalse();
    }

    [Fact]
    public void Stores_start_in_utc()
    {
        var parisTime = new DateTimeOffset(2026, 12, 24, 20, 0, 0, TimeSpan.FromHours(1));

        var evt = CreateEvent(startsAt: parisTime).Value;

        evt.StartsAt.Offset.ShouldBe(TimeSpan.Zero);
        evt.StartsAt.ShouldBe(parisTime); // même instant
    }

    [Fact]
    public void Rejects_start_in_the_past()
    {
        CreateEvent(startsAt: Now.AddMinutes(-1)).Error.ShouldBe(DomainErrors.StartInPast);
    }

    [Fact]
    public void Rejects_start_equal_to_now()
    {
        CreateEvent(startsAt: Now).Error.ShouldBe(DomainErrors.StartInPast);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    [InlineData(59)]
    public void Rejects_duration_shorter_than_one_minute(int seconds)
    {
        CreateEvent(duration: TimeSpan.FromSeconds(seconds)).Error.ShouldBe(DomainErrors.InvalidDuration);
    }

    [Fact]
    public void Accepts_duration_of_one_minute()
    {
        CreateEvent(duration: TimeSpan.FromMinutes(1)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Duration_is_optional()
    {
        CreateEvent(duration: null).Value.Duration.ShouldBeNull();
    }
}
