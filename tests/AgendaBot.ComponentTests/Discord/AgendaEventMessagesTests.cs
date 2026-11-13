using AgendaBot.Discord.Presentation;
using AgendaBot.Domain;
using AgendaBot.Testing.Builders;

namespace AgendaBot.ComponentTests.Discord;

public class AgendaEventMessagesTests
{
    private const string TitleTryingToPingEveryone = "@everyone @here <@&123456789012345678> <@987654321098765432>";

    [Fact]
    public void Reminder_gives_title_and_start_relative_to_now_and_as_time()
    {
        var evt = new AnAgendaEvent().Titled("Réunion").BuildPersisted(7);
        var unix = evt.StartsAt.ToUnixTimeSeconds();

        AgendaEventMessages.Reminder(evt).Text.ShouldBe($"⏰ Rappel : **Réunion** commence <t:{unix}:R> (<t:{unix}:t>).");
    }

    [Fact]
    public void Reminder_of_all_day_event_gives_title_and_day_without_time()
    {
        var dayStartsAt = new DateTimeOffset(2026, 12, 23, 23, 0, 0, TimeSpan.Zero);
        var evt = new AnAgendaEvent().Titled("Réveillon").OnWholeDay(dayStartsAt, dayStartsAt.AddDays(1)).BuildPersisted(7);
        var unix = dayStartsAt.ToUnixTimeSeconds();

        AgendaEventMessages.Reminder(evt).Text.ShouldBe($"⏰ Rappel : **Réveillon** a lieu toute la journée le <t:{unix}:D>.");
    }

    [Fact]
    public void Reminder_pings_nobody_even_when_the_title_contains_mentions()
    {
        var evt = new AnAgendaEvent().Titled(TitleTryingToPingEveryone).BuildPersisted(7);

        AgendaEventMessages.Reminder(evt).ShouldPingNobody();
    }

    [Fact]
    public void Deletion_confirmation_gives_id_and_title()
    {
        var evt = new AnAgendaEvent().Titled("Réunion").BuildPersisted(7);

        AgendaEventMessages.Deleted(evt).Text.ShouldBe("🗑️ Événement #7 « Réunion » supprimé.");
    }

    [Fact]
    public void Deletion_confirmation_pings_nobody_even_when_the_title_contains_mentions()
    {
        var evt = new AnAgendaEvent().Titled(TitleTryingToPingEveryone).BuildPersisted(7);

        AgendaEventMessages.Deleted(evt).ShouldPingNobody();
    }

    [Fact]
    public void Error_shows_the_message_meant_for_the_user()
    {
        AgendaEventMessages.Error(DomainErrors.TitleRequired).ShouldBe("❌ Le titre est obligatoire.");
    }

    [Fact]
    public void Invalid_date_explains_both_expected_formats_with_examples()
    {
        AgendaEventMessages.InvalidDate.ShouldBe(
            "❌ Date invalide. Format attendu : `dd/MM/yyyy HH:mm` (ex. `24/12/2026 20:00`), " +
            "ou `dd/MM/yyyy` pour toute la journée (ex. `24/12/2026`).");
    }
}
