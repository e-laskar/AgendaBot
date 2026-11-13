using AgendaBot.Domain;
using AgendaBot.Testing.Builders;

namespace AgendaBot.UnitTests.Domain;

public class AgendaEventEditTests
{
    private static readonly DateTimeOffset Now = AnAgendaEvent.DefaultNow;
    private static readonly DateTimeOffset Tomorrow = Now.AddDays(1);
    private static readonly DateTimeOffset TomorrowStartsAt = new(2026, 11, 4, 23, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TomorrowEndsAt = TomorrowStartsAt.AddDays(1);

    private static readonly EventTitle NewTitle = EventTitle.Create("Nouveau titre").Value;
    private static readonly EventDescription NewDescription = EventDescription.CreateOptional("Nouvelle description").Value!;

    private static AgendaEvent AnUpcomingEvent(bool reminderSent = false) =>
        new AnAgendaEvent()
            .InChannel(new ChannelId(20)).CreatedBy(new UserId(30))
            .Titled("Réunion").DescribedAs("Salle 2")
            .StartingAt(Tomorrow).Lasting(TimeSpan.FromMinutes(30))
            .BuildPersisted(7, reminderSent);

    private static AgendaEvent APastEvent() =>
        new AnAgendaEvent().StartingAt(Now.AddMinutes(-1)).BuildPersisted(7);

    [Fact]
    public void Describing_changes_title_and_description_and_keeps_the_rest()
    {
        var original = AnUpcomingEvent(reminderSent: true);

        var edited = original.Describe(NewTitle, NewDescription, Now).Value;

        edited.ShouldSatisfyAllConditions(
            () => edited.Title.ShouldBe(NewTitle),
            () => edited.Description.ShouldBe(NewDescription),
            () => edited.Id.ShouldBe(original.Id),
            () => edited.GuildId.ShouldBe(original.GuildId),
            () => edited.ChannelId.ShouldBe(original.ChannelId),
            () => edited.CreatedBy.ShouldBe(original.CreatedBy),
            () => edited.StartsAt.ShouldBe(original.StartsAt),
            () => edited.Duration.ShouldBe(original.Duration),
            () => edited.IsAllDay.ShouldBe(original.IsAllDay),
            () => edited.ReminderSent.ShouldBeTrue());
    }

    [Fact]
    public void Describing_can_remove_the_description()
    {
        var edited = AnUpcomingEvent().Describe(NewTitle, description: null, Now).Value;

        edited.Description.ShouldBeNull();
    }

    [Fact]
    public void Rejects_describing_an_event_already_past()
    {
        APastEvent().Describe(NewTitle, NewDescription, Now).Error.ShouldBe(DomainErrors.EventAlreadyPast);
    }

    [Fact]
    public void Rescheduling_changes_start_and_duration_and_keeps_the_rest()
    {
        var original = AnUpcomingEvent();

        var edited = original.Reschedule(Tomorrow.AddHours(2), TimeSpan.FromHours(1), Now).Value;

        edited.ShouldSatisfyAllConditions(
            () => edited.StartsAt.ShouldBe(Tomorrow.AddHours(2)),
            () => edited.Duration.ShouldBe(TimeSpan.FromHours(1)),
            () => edited.IsAllDay.ShouldBeFalse(),
            () => edited.Id.ShouldBe(original.Id),
            () => edited.Title.ShouldBe(original.Title),
            () => edited.Description.ShouldBe(original.Description),
            () => edited.CreatedBy.ShouldBe(original.CreatedBy));
    }

    [Fact]
    public void Rescheduling_to_another_start_makes_the_reminder_due_again()
    {
        var edited = AnUpcomingEvent(reminderSent: true).Reschedule(Tomorrow.AddHours(2), null, Now).Value;

        edited.ReminderSent.ShouldBeFalse();
    }

    [Fact]
    public void Changing_only_the_duration_keeps_the_reminder_state()
    {
        var edited = AnUpcomingEvent(reminderSent: true).Reschedule(Tomorrow, TimeSpan.FromHours(2), Now).Value;

        edited.ReminderSent.ShouldBeTrue();
    }

    [Fact]
    public void Rejects_rescheduling_before_current_time()
    {
        AnUpcomingEvent().Reschedule(Now, null, Now).Error.ShouldBe(DomainErrors.StartInPast);
    }

    [Fact]
    public void Rejects_rescheduling_with_a_duration_under_one_minute()
    {
        AnUpcomingEvent().Reschedule(Tomorrow, TimeSpan.FromSeconds(30), Now).Error.ShouldBe(DomainErrors.InvalidDuration);
    }

    [Fact]
    public void Rejects_rescheduling_an_event_already_past()
    {
        APastEvent().Reschedule(Tomorrow, null, Now).Error.ShouldBe(DomainErrors.EventAlreadyPast);
    }

    [Fact]
    public void Rescheduling_on_a_whole_day_makes_the_event_all_day()
    {
        var edited = AnUpcomingEvent(reminderSent: true).RescheduleAllDay(TomorrowStartsAt, TomorrowEndsAt, Now).Value;

        edited.ShouldSatisfyAllConditions(
            () => edited.IsAllDay.ShouldBeTrue(),
            () => edited.StartsAt.ShouldBe(TomorrowStartsAt),
            () => edited.Duration.ShouldBe(TimeSpan.FromDays(1)),
            () => edited.ReminderSent.ShouldBeFalse());
    }

    [Fact]
    public void Rescheduling_an_all_day_event_at_a_time_makes_it_timed()
    {
        var allDay = new AnAgendaEvent().OnWholeDay(TomorrowStartsAt, TomorrowEndsAt).BuildPersisted(7);

        var edited = allDay.Reschedule(Tomorrow, null, Now).Value;

        edited.ShouldSatisfyAllConditions(
            () => edited.IsAllDay.ShouldBeFalse(),
            () => edited.StartsAt.ShouldBe(Tomorrow),
            () => edited.Duration.ShouldBeNull());
    }

    [Fact]
    public void Rejects_rescheduling_on_a_day_already_over()
    {
        AnUpcomingEvent().RescheduleAllDay(TomorrowStartsAt.AddDays(-2), TomorrowStartsAt.AddDays(-1), Now)
            .Error.ShouldBe(DomainErrors.DayInPast);
    }

    [Fact]
    public void Editing_does_not_change_the_original_event()
    {
        var original = AnUpcomingEvent();

        original.Describe(NewTitle, NewDescription, Now);
        original.Reschedule(Tomorrow.AddHours(2), null, Now);

        original.Title.Value.ShouldBe("Réunion");
        original.StartsAt.ShouldBe(Tomorrow);
    }
}
