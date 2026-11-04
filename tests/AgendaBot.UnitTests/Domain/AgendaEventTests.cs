using AgendaBot.Domain;
using AgendaBot.UnitTests.Builders;

namespace AgendaBot.UnitTests.Domain;

public class AgendaEventTests
{
    private static readonly DateTimeOffset Now = AnAgendaEvent.DefaultNow;

    [Fact]
    public void Creates_event_with_given_values()
    {
        var startsAt = Now.AddDays(1);

        var evt = new AnAgendaEvent()
            .InGuild(new GuildId(1)).InChannel(new ChannelId(2)).CreatedBy(new UserId(3))
            .Titled("Réunion").DescribedAs("Salle 2")
            .StartingAt(startsAt).Lasting(TimeSpan.FromMinutes(90))
            .Build();

        evt.ShouldSatisfyAllConditions(
            () => evt.Id.ShouldBe(0),
            () => evt.GuildId.ShouldBe(new GuildId(1)),
            () => evt.ChannelId.ShouldBe(new ChannelId(2)),
            () => evt.CreatedBy.ShouldBe(new UserId(3)),
            () => evt.Title.Value.ShouldBe("Réunion"),
            () => evt.Description!.Value.ShouldBe("Salle 2"),
            () => evt.StartsAt.ShouldBe(startsAt),
            () => evt.Duration.ShouldBe(TimeSpan.FromMinutes(90)));
    }

    [Fact]
    public void New_event_has_no_reminder_sent()
    {
        new AnAgendaEvent().Build().ReminderSent.ShouldBeFalse();
    }

    [Fact]
    public void Stores_start_in_utc()
    {
        var parisTime = new DateTimeOffset(2026, 12, 24, 20, 0, 0, TimeSpan.FromHours(1));

        var evt = new AnAgendaEvent().StartingAt(parisTime).Build();

        evt.StartsAt.Offset.ShouldBe(TimeSpan.Zero);
        evt.StartsAt.ShouldBe(parisTime);
    }

    [Fact]
    public void Rejects_start_in_the_past()
    {
        new AnAgendaEvent().StartingAt(Now.AddMinutes(-1)).CreatedAt(Now)
            .TryBuild().Error.ShouldBe(DomainErrors.StartInPast);
    }

    [Fact]
    public void Rejects_start_equal_to_now()
    {
        new AnAgendaEvent().StartingAt(Now).CreatedAt(Now)
            .TryBuild().Error.ShouldBe(DomainErrors.StartInPast);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    [InlineData(59)]
    public void Rejects_duration_shorter_than_one_minute(int seconds)
    {
        new AnAgendaEvent().Lasting(TimeSpan.FromSeconds(seconds))
            .TryBuild().Error.ShouldBe(DomainErrors.InvalidDuration);
    }

    [Fact]
    public void Accepts_duration_of_one_minute()
    {
        new AnAgendaEvent().Lasting(TimeSpan.FromMinutes(1)).TryBuild().IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Duration_is_optional()
    {
        new AnAgendaEvent().Lasting(null).Build().Duration.ShouldBeNull();
    }
}
