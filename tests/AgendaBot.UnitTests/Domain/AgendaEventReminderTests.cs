using AgendaBot.Domain;
using AgendaBot.UnitTests.Builders;

namespace AgendaBot.UnitTests.Domain;

public class AgendaEventReminderTests
{
    private static readonly DateTimeOffset Now = AnAgendaEvent.DefaultNow;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private static AgendaEvent EventStartingAt(DateTimeOffset startsAt) =>
        new AnAgendaEvent().StartingAt(startsAt).CreatedAt(Now).Build();

    [Fact]
    public void Reminder_is_due_when_event_starts_within_window()
    {
        EventStartingAt(Now.AddMinutes(10)).IsReminderDue(Now, Window).ShouldBeTrue();
    }

    [Fact]
    public void Reminder_is_due_when_event_starts_exactly_at_window_end()
    {
        EventStartingAt(Now.Add(Window)).IsReminderDue(Now, Window).ShouldBeTrue();
    }

    [Fact]
    public void Reminder_is_not_due_when_event_starts_after_window()
    {
        EventStartingAt(Now.Add(Window).AddMinutes(1)).IsReminderDue(Now, Window).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]   // commence pile maintenant
    [InlineData(5)]   // a commencé il y a 5 min
    public void Reminder_is_not_due_when_event_has_started(int minutesSinceStart)
    {
        var evt = EventStartingAt(Now.AddMinutes(1));
        var later = evt.StartsAt.AddMinutes(minutesSinceStart);

        evt.IsReminderDue(later, Window).ShouldBeFalse();
    }

    [Fact]
    public void Reminder_is_not_due_once_sent()
    {
        var evt = EventStartingAt(Now.AddMinutes(10));

        evt.MarkReminderSent();

        evt.IsReminderDue(Now, Window).ShouldBeFalse();
    }

    [Fact]
    public void Marking_reminder_sent_records_it()
    {
        var evt = EventStartingAt(Now.AddMinutes(10));

        evt.MarkReminderSent();

        evt.ReminderSent.ShouldBeTrue();
    }

    [Fact]
    public void Marking_reminder_sent_twice_is_harmless()
    {
        var evt = EventStartingAt(Now.AddMinutes(10));

        evt.MarkReminderSent();
        Should.NotThrow(evt.MarkReminderSent);

        evt.ReminderSent.ShouldBeTrue();
    }
}
