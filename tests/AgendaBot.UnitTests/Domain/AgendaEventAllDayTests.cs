using AgendaBot.Domain;
using AgendaBot.Testing.Builders;

namespace AgendaBot.UnitTests.Domain;

public class AgendaEventAllDayTests
{
    private static readonly DateTimeOffset Now = AnAgendaEvent.DefaultNow;
    private static readonly DateTimeOffset TodayStartsAt = new(2026, 11, 3, 23, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TodayEndsAt = TodayStartsAt.AddDays(1);

    [Fact]
    public void Creates_all_day_event_lasting_the_whole_day()
    {
        var evt = new AnAgendaEvent().OnWholeDay(TodayStartsAt.AddDays(1), TodayEndsAt.AddDays(1)).Build();

        evt.ShouldSatisfyAllConditions(
            () => evt.IsAllDay.ShouldBeTrue(),
            () => evt.StartsAt.ShouldBe(TodayStartsAt.AddDays(1)),
            () => evt.Duration.ShouldBe(TimeSpan.FromDays(1)));
    }

    [Fact]
    public void Event_created_with_a_time_is_not_all_day()
    {
        new AnAgendaEvent().Build().IsAllDay.ShouldBeFalse();
    }

    [Fact]
    public void Accepts_all_day_event_for_the_current_day()
    {
        new AnAgendaEvent().OnWholeDay(TodayStartsAt, TodayEndsAt).CreatedAt(Now)
            .TryBuild().IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Rejects_all_day_event_for_a_day_already_over()
    {
        new AnAgendaEvent().OnWholeDay(TodayStartsAt.AddDays(-1), TodayStartsAt).CreatedAt(Now)
            .TryBuild().Error.ShouldBe(DomainErrors.DayInPast);
    }

    [Fact]
    public void Rejects_all_day_event_ending_before_it_starts()
    {
        new AnAgendaEvent().OnWholeDay(TodayEndsAt, TodayStartsAt).CreatedAt(Now)
            .TryBuild().Error.ShouldBe(DomainErrors.InvalidDuration);
    }

    [Fact]
    public void All_day_event_is_upcoming_until_its_day_is_over()
    {
        var evt = new AnAgendaEvent().OnWholeDay(TodayStartsAt, TodayEndsAt).BuildPersisted(1);

        evt.ShouldSatisfyAllConditions(
            () => evt.IsUpcomingAt(TodayStartsAt).ShouldBeTrue(),
            () => evt.IsUpcomingAt(TodayEndsAt.AddTicks(-1)).ShouldBeTrue(),
            () => evt.IsUpcomingAt(TodayEndsAt).ShouldBeFalse());
    }

    [Fact]
    public void Timed_event_is_upcoming_until_it_starts()
    {
        var evt = new AnAgendaEvent().StartingAt(Now).Lasting(TimeSpan.FromHours(2)).BuildPersisted(1);

        evt.ShouldSatisfyAllConditions(
            () => evt.IsUpcomingAt(Now).ShouldBeTrue(),
            () => evt.IsUpcomingAt(Now.AddTicks(1)).ShouldBeFalse());
    }
}
