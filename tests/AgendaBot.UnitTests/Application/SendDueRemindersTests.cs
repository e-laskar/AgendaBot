using AgendaBot.Application.UseCases;
using AgendaBot.UnitTests.Application.Fakes;
using AgendaBot.UnitTests.Builders;
using Microsoft.Extensions.Time.Testing;

namespace AgendaBot.UnitTests.Application;

public class SendDueRemindersTests
{
    private static readonly DateTimeOffset Now = AnAgendaEvent.DefaultNow;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly InMemoryEventRepository _repository = new();
    private readonly SpyReminderNotifier _notifier = new();
    private readonly FakeTimeProvider _time = new(Now);

    private SendDueReminders CreateUseCase(TimeSpan? window = null) =>
        new(_repository, _notifier, _time, new ReminderSettings(window ?? Window));

    [Fact]
    public async Task Notifies_events_starting_within_window()
    {
        _repository.Seed(
            new AnAgendaEvent().StartingAt(Now.AddMinutes(10)).BuildPersisted(1),
            new AnAgendaEvent().StartingAt(Now.AddMinutes(20)).BuildPersisted(2));

        await CreateUseCase().HandleAsync();

        _notifier.Notified.ShouldHaveSingleItem().Id.ShouldBe(1);
    }

    [Fact]
    public async Task Uses_configured_window()
    {
        _repository.Seed(new AnAgendaEvent().StartingAt(Now.AddMinutes(20)).BuildPersisted(1));

        await CreateUseCase(window: TimeSpan.FromMinutes(30)).HandleAsync();

        _notifier.Notified.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Ignores_events_already_started()
    {
        _repository.Seed(new AnAgendaEvent().StartingAt(Now.AddMinutes(-1)).BuildPersisted(1));

        await CreateUseCase().HandleAsync();

        _notifier.Notified.ShouldBeEmpty();
    }

    [Fact]
    public async Task Marks_notified_events_as_reminded()
    {
        _repository.Seed(new AnAgendaEvent().StartingAt(Now.AddMinutes(10)).BuildPersisted(1));

        await CreateUseCase().HandleAsync();

        _repository.All.ShouldHaveSingleItem().ReminderSent.ShouldBeTrue();
    }

    [Fact]
    public async Task Does_not_notify_twice()
    {
        _repository.Seed(new AnAgendaEvent().StartingAt(Now.AddMinutes(10)).BuildPersisted(1));
        var sendDueReminders = CreateUseCase();

        await sendDueReminders.HandleAsync();
        _time.Advance(TimeSpan.FromMinutes(1));
        await sendDueReminders.HandleAsync();

        _notifier.Notified.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Reports_number_of_reminders_sent()
    {
        _repository.Seed(
            new AnAgendaEvent().StartingAt(Now.AddMinutes(5)).BuildPersisted(1),
            new AnAgendaEvent().StartingAt(Now.AddMinutes(10)).BuildPersisted(2));

        var report = await CreateUseCase().HandleAsync();

        report.Sent.ShouldBe(2);
        report.Failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_failing_reminder_does_not_block_the_others()
    {
        _repository.Seed(
            new AnAgendaEvent().StartingAt(Now.AddMinutes(5)).BuildPersisted(1),
            new AnAgendaEvent().StartingAt(Now.AddMinutes(10)).BuildPersisted(2));
        _notifier.FailFor(1);

        var report = await CreateUseCase().HandleAsync();

        _notifier.Notified.ShouldHaveSingleItem().Id.ShouldBe(2);
        report.Sent.ShouldBe(1);
        report.Failures.ShouldHaveSingleItem().Event.Id.ShouldBe(1);
    }

    [Fact]
    public async Task A_failed_reminder_is_retried_next_time()
    {
        _repository.Seed(new AnAgendaEvent().StartingAt(Now.AddMinutes(10)).BuildPersisted(1));
        var sendDueReminders = CreateUseCase();
        _notifier.FailFor(1);
        await sendDueReminders.HandleAsync();

        _notifier.Recover();
        _time.Advance(TimeSpan.FromMinutes(1));
        await sendDueReminders.HandleAsync();

        _notifier.Notified.ShouldHaveSingleItem().Id.ShouldBe(1);
        _repository.All.ShouldHaveSingleItem().ReminderSent.ShouldBeTrue();
    }
}
