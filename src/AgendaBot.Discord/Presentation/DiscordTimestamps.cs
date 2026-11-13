namespace AgendaBot.Discord.Presentation;

internal static class DiscordTimestamps
{
    public static string RelativeToViewer(DateTimeOffset instant) => Format(instant, "R");

    public static string FullDateInViewerTimeZone(DateTimeOffset instant) => Format(instant, "F");

    public static string LongDateInViewerTimeZone(DateTimeOffset instant) => Format(instant, "D");

    public static string TimeInViewerTimeZone(DateTimeOffset instant) => Format(instant, "t");

    private static string Format(DateTimeOffset instant, string style) =>
        $"<t:{instant.ToUnixTimeSeconds()}:{style}>";
}
