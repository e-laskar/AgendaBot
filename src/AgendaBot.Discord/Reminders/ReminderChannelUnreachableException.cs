using AgendaBot.Domain;

namespace AgendaBot.Discord.Reminders;

public sealed class ReminderChannelUnreachableException(ChannelId channel)
    : Exception($"Salon {channel.Value} introuvable ou inaccessible : rappel non envoyé.")
{
    public ChannelId Channel { get; } = channel;
}
