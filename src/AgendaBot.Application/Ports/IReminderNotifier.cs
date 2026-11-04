using AgendaBot.Domain;

namespace AgendaBot.Application.Ports;

public interface IReminderNotifier
{
    Task NotifyAsync(AgendaEvent evt, CancellationToken cancellationToken = default);
}
