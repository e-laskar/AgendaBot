using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Application.UseCases;

public sealed record ReminderSettings(TimeSpan Window);

public sealed record ReminderFailure(AgendaEvent Event, Exception Exception);

public sealed record RemindersReport(int Sent, IReadOnlyList<ReminderFailure> Failures);

public sealed class SendDueReminders(
    IEventRepository repository,
    IReminderNotifier notifier,
    TimeProvider time,
    ReminderSettings settings)
{
    public async Task<RemindersReport> HandleAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var dueEvents = await FindDueEventsAsync(now, cancellationToken);

        var sent = 0;
        var failures = new List<ReminderFailure>();
        foreach (var evt in dueEvents)
        {
            try
            {
                await NotifyAndMarkAsRemindedAsync(evt, cancellationToken);
                sent++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures.Add(new ReminderFailure(evt, exception));
            }
        }

        return new RemindersReport(sent, failures);
    }

    private async Task<IEnumerable<AgendaEvent>> FindDueEventsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var candidates = await repository.GetWithoutReminderStartingBetweenAsync(
            now, now + settings.Window, cancellationToken);
        return candidates.Where(evt => evt.IsReminderDue(now, settings.Window));
    }

    private async Task NotifyAndMarkAsRemindedAsync(AgendaEvent evt, CancellationToken cancellationToken)
    {
        await notifier.NotifyAsync(evt, cancellationToken);
        evt.MarkReminderSent();
        await repository.SaveReminderSentAsync(evt, cancellationToken);
    }
}
