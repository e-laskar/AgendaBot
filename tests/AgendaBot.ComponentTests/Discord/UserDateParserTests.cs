using AgendaBot.Discord.Commands;

namespace AgendaBot.ComponentTests.Discord;

public class UserDateParserTests
{
    private static readonly UserDateParser InParis = new(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);

    [Fact]
    public void Reads_winter_date_in_user_time_zone()
    {
        InParis.TryParse("24/12/2026 20:00", out var startsAt).ShouldBeTrue();

        startsAt.ShouldBe(Utc(2026, 12, 24, 19, 0));
    }

    [Fact]
    public void Reads_summer_date_in_user_time_zone()
    {
        InParis.TryParse("14/07/2026 20:00", out var startsAt).ShouldBeTrue();

        startsAt.ShouldBe(Utc(2026, 7, 14, 18, 0));
    }

    [Fact]
    public void Returns_date_in_utc()
    {
        InParis.TryParse("24/12/2026 20:00", out var startsAt);

        startsAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Uses_the_configured_time_zone()
    {
        var inNewYork = new UserDateParser(TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));

        inNewYork.TryParse("24/12/2026 20:00", out var startsAt).ShouldBeTrue();

        startsAt.ShouldBe(Utc(2026, 12, 25, 1, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("24/12/2026")]
    [InlineData("2026-12-24 20:00")]
    [InlineData("24/12/2026 20h00")]
    [InlineData("12/24/2026 20:00")]
    [InlineData("31/02/2026 10:00")]
    public void Rejects_text_not_matching_expected_format(string text)
    {
        InParis.TryParse(text, out _).ShouldBeFalse();
    }

    [Fact]
    public void Rejects_time_skipped_when_clocks_go_forward()
    {
        InParis.TryParse("29/03/2026 02:30", out _).ShouldBeFalse();
    }

    [Fact]
    public void Reads_time_repeated_when_clocks_go_back_as_winter_time()
    {
        InParis.TryParse("25/10/2026 02:30", out var startsAt).ShouldBeTrue();

        startsAt.ShouldBe(Utc(2026, 10, 25, 1, 30));
    }
}
