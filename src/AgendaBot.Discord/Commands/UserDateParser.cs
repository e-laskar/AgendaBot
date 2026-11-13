using System.Globalization;
using AgendaBot.Application.UseCases;

namespace AgendaBot.Discord.Commands;

public sealed class UserDateParser(TimeZoneInfo userTimeZone)
{
    public const string Format = "dd/MM/yyyy HH:mm";
    public const string Example = "24/12/2026 20:00";
    public const string AllDayFormat = "dd/MM/yyyy";
    public const string AllDayExample = "24/12/2026";

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public bool TryParse(string text, out EventDate date)
    {
        date = null!;
        if (TryParseExact(text, Format, out var localDateTime))
        {
            if (IsSkippedWhenClocksGoForward(localDateTime))
                return false;

            date = new EventDate.AtTime(ToUtc(localDateTime));
            return true;
        }

        if (TryParseExact(text, AllDayFormat, out var localDay))
        {
            date = new EventDate.AllDay(StartOfDay(localDay), StartOfDay(localDay.AddDays(1)));
            return true;
        }

        return false;
    }

    private static bool TryParseExact(string text, string format, out DateTime localDate)
    {
        var parsed = DateTime.TryParseExact(text, format, French, DateTimeStyles.None, out localDate);
        localDate = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        return parsed;
    }

    private DateTimeOffset StartOfDay(DateTime localDay)
    {
        var firstExistingTime = localDay;
        while (IsSkippedWhenClocksGoForward(firstExistingTime))
            firstExistingTime = firstExistingTime.AddMinutes(1);
        return ToUtc(firstExistingTime);
    }

    private DateTimeOffset ToUtc(DateTime localDate) =>
        new(TimeZoneInfo.ConvertTimeToUtc(localDate, userTimeZone));

    private bool IsSkippedWhenClocksGoForward(DateTime localDate) => userTimeZone.IsInvalidTime(localDate);
}
