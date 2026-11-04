using Discord;

namespace AgendaBot.Discord.Commands;

internal static class DeferredInteractionReplies
{
    public static async Task ReplacePublicDeferralWithPrivateMessageAsync(this IDiscordInteraction interaction, string message)
    {
        await interaction.DeleteOriginalResponseAsync();
        await interaction.FollowupAsync(message, ephemeral: true);
    }
}
