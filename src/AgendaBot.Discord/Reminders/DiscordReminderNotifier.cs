using AgendaBot.Application.Ports;
using AgendaBot.Discord.Presentation;
using AgendaBot.Domain;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace AgendaBot.Discord.Reminders;

public sealed class DiscordReminderNotifier(DiscordSocketClient client, ILogger<DiscordReminderNotifier> logger)
    : IReminderNotifier
{
    public async Task NotifyAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        if (client.GetChannel(evt.ChannelId.Value) is not IMessageChannel channel)
        {
            logger.LogWarning(
                "Salon {ChannelId} introuvable, rappel abandonné pour l'événement {EventId}",
                evt.ChannelId.Value, evt.Id);
            return;
        }

        await channel.SendMessageAsync(
            ReminderMessage(evt),
            options: new RequestOptions { CancelToken = cancellationToken });
    }

    private static string ReminderMessage(AgendaEvent evt) =>
        $"⏰ Rappel : **{evt.Title}** commence {DiscordTimestamps.RelativeToViewer(evt.StartsAt)} " +
        $"({DiscordTimestamps.TimeInViewerTimeZone(evt.StartsAt)}).";
}
