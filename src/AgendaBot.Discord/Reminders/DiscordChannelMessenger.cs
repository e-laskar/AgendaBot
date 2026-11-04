using AgendaBot.Discord.Presentation;
using AgendaBot.Domain;
using Discord;
using Discord.WebSocket;

namespace AgendaBot.Discord.Reminders;

internal sealed class DiscordChannelMessenger(DiscordSocketClient client) : IChannelMessenger
{
    public async Task<bool> TrySendAsync(ChannelId channel, OutgoingMessage message, CancellationToken cancellationToken)
    {
        var options = new RequestOptions { CancelToken = cancellationToken };
        if (await FromCacheOrApiAsync(channel, options) is not IMessageChannel messageChannel)
            return false;

        await messageChannel.SendMessageAsync(message.Text, allowedMentions: message.AllowedMentions, options: options);
        return true;
    }

    private async Task<IChannel?> FromCacheOrApiAsync(ChannelId channel, RequestOptions options) =>
        await client.GetChannelAsync(channel.Value, options);
}
