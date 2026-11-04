using AgendaBot.Application.Ports;
using AgendaBot.Discord.Presentation;
using AgendaBot.Domain;

namespace AgendaBot.Discord.Reminders;

internal sealed class DiscordReminderNotifier(IChannelMessenger messenger) : IReminderNotifier
{
    public async Task NotifyAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        if (!await messenger.TrySendAsync(evt.ChannelId, AgendaEventMessages.Reminder(evt), cancellationToken))
            throw new ReminderChannelUnreachableException(evt.ChannelId);
    }
}
