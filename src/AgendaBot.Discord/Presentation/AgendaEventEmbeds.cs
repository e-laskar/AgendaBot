using AgendaBot.Domain;
using Discord;

namespace AgendaBot.Discord.Presentation;

internal static class AgendaEventEmbeds
{
    public static Embed Details(AgendaEvent evt)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"📌 {evt.Title}")
            .WithDescription(evt.Description?.Value ?? "_Pas de description_")
            .WithColor(Color.Green)
            .AddField("Début", $"{DiscordTimestamps.FullDateInViewerTimeZone(evt.StartsAt)}\n{DiscordTimestamps.RelativeToViewer(evt.StartsAt)}", inline: true)
            .AddField("Créé par", MentionUtils.MentionUser(evt.CreatedBy.Value), inline: true)
            .WithFooter($"ID : {evt.Id}");

        if (evt.Duration is { } duration)
            embed.AddField("Durée", HoursAndMinutes(duration), inline: true);

        return embed.Build();
    }

    public static Embed UpcomingList(IReadOnlyList<AgendaEvent> events)
    {
        var embed = new EmbedBuilder()
            .WithTitle("📅 Prochains événements")
            .WithColor(Color.Blue);

        foreach (var evt in events)
        {
            embed.AddField(
                $"#{evt.Id} · {evt.Title}",
                $"{DiscordTimestamps.FullDateInViewerTimeZone(evt.StartsAt)} ({DiscordTimestamps.RelativeToViewer(evt.StartsAt)})");
        }

        return embed.Build();
    }

    private static string HoursAndMinutes(TimeSpan duration) =>
        $"{(int)duration.TotalHours}h{duration.Minutes:00}";
}
