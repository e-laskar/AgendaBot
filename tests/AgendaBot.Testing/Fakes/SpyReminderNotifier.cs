using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Testing.Fakes;

public sealed class SpyReminderNotifier : IReminderNotifier
{
    private readonly List<AgendaEvent> _notified = [];
    private readonly HashSet<int> _failingEventIds = [];
    private readonly HashSet<int> _interruptedEventIds = [];

    public IReadOnlyList<AgendaEvent> Notified => _notified;

    public void FailFor(int eventId) => _failingEventIds.Add(eventId);

    public void InterruptByShutdownFor(int eventId) => _interruptedEventIds.Add(eventId);

    public void Recover()
    {
        _failingEventIds.Clear();
        _interruptedEventIds.Clear();
    }

    public Task NotifyAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        if (_failingEventIds.Contains(evt.Id))
            throw new InvalidOperationException($"Envoi impossible pour l'événement {evt.Id}.");
        if (_interruptedEventIds.Contains(evt.Id))
            throw new OperationCanceledException($"Arrêt du bot pendant l'envoi pour l'événement {evt.Id}.");

        _notified.Add(evt);
        return Task.CompletedTask;
    }
}
