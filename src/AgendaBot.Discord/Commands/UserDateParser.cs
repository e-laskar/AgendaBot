using System.Globalization;

namespace AgendaBot.Discord.Commands;

public sealed class UserDateParser(TimeZoneInfo userTimeZone)
{
    public const string Format = "dd/MM/yyyy HH:mm";
    public const string Example = "24/12/2026 20:00";

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public bool TryParse(string text, out DateTimeOffset startsAt)
    {
        startsAt = default;
        if (!DateTime.TryParseExact(text, Format, French, DateTimeStyles.None, out var parsed))
            return false;

        var localDate = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
        if (IsSkippedWhenClocksGoForward(localDate))
            return false;

        startsAt = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localDate, userTimeZone));
        return true;
    }

    private bool IsSkippedWhenClocksGoForward(DateTime localDate) => userTimeZone.IsInvalidTime(localDate);
}
