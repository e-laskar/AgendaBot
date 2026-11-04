using AgendaBot.Domain;

namespace AgendaBot.Application.Ports;

/// <summary>Port sortant : prévient les participants qu'un événement va commencer.</summary>
public interface IReminderNotifier
{
    Task NotifyAsync(AgendaEvent evt, CancellationToken cancellationToken = default);
}
