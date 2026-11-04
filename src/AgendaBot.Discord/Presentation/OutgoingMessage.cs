using Discord;

namespace AgendaBot.Discord.Presentation;

internal sealed record OutgoingMessage(string Text, AllowedMentions AllowedMentions)
{
    public static OutgoingMessage PingingNobody(string text) =>
        new(text, new AllowedMentions(AllowedMentionTypes.None));
}
