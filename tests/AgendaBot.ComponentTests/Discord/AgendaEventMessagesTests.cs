using AgendaBot.Discord.Presentation;
using AgendaBot.Domain;
using AgendaBot.Testing.Builders;

namespace AgendaBot.ComponentTests.Discord;

public class AgendaEventMessagesTests
{
    [Fact]
    public void Reminder_gives_title_and_start_relative_to_now_and_as_time()
    {
        var evt = new AnAgendaEvent().Titled("Réunion").BuildPersisted(7);
        var unix = evt.StartsAt.ToUnixTimeSeconds();

        AgendaEventMessages.Reminder(evt).ShouldBe($"⏰ Rappel : **Réunion** commence <t:{unix}:R> (<t:{unix}:t>).");
    }

    [Fact]
    public void Deletion_confirmation_gives_id_and_title()
    {
        var evt = new AnAgendaEvent().Titled("Réunion").BuildPersisted(7);

        AgendaEventMessages.Deleted(evt).ShouldBe("🗑️ Événement #7 « Réunion » supprimé.");
    }

    [Fact]
    public void Error_shows_the_message_meant_for_the_user()
    {
        AgendaEventMessages.Error(DomainErrors.TitleRequired).ShouldBe("❌ Le titre est obligatoire.");
    }

    [Fact]
    public void Invalid_date_explains_expected_format_with_an_example()
    {
        AgendaEventMessages.InvalidDate.ShouldBe("❌ Date invalide. Format attendu : `dd/MM/yyyy HH:mm` (ex. `24/12/2026 20:00`).");
    }
}
