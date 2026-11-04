using AgendaBot.Discord.Presentation;
using AgendaBot.Domain;

namespace AgendaBot.Discord.Reminders;

internal interface IChannelMessenger
{
    Task<bool> TrySendAsync(ChannelId channel, OutgoingMessage message, CancellationToken cancellationToken);
}
