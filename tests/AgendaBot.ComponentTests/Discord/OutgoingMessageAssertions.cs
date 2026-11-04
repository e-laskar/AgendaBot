using AgendaBot.Discord.Presentation;
using Discord;

namespace AgendaBot.ComponentTests.Discord;

internal static class OutgoingMessageAssertions
{
    public static void ShouldPingNobody(this OutgoingMessage message)
    {
        var mentions = message.AllowedMentions;
        mentions.ShouldSatisfyAllConditions(
            () => mentions.AllowedTypes.ShouldBe(AllowedMentionTypes.None),
            () => (mentions.UserIds ?? []).ShouldBeEmpty(),
            () => (mentions.RoleIds ?? []).ShouldBeEmpty(),
            () => (mentions.MentionRepliedUser ?? false).ShouldBeFalse());
    }
}
