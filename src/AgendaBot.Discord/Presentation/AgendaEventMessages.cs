using AgendaBot.Discord.Commands;
using AgendaBot.Domain;

namespace AgendaBot.Discord.Presentation;

internal static class AgendaEventMessages
{
    public const string Added = "✅ Événement ajouté !";
    public const string NoUpcomingEvents = "📭 Aucun événement à venir.";

    public static readonly string InvalidDate =
        Error($"Date invalide. Format attendu : `{UserDateParser.Format}` (ex. `{UserDateParser.Example}`).");

    public static OutgoingMessage Reminder(AgendaEvent evt) =>
        OutgoingMessage.PingingNobody(
            $"⏰ Rappel : **{evt.Title}** commence {DiscordTimestamps.RelativeToViewer(evt.StartsAt)} " +
            $"({DiscordTimestamps.TimeInViewerTimeZone(evt.StartsAt)}).");

    public static OutgoingMessage Deleted(AgendaEvent evt) =>
        OutgoingMessage.PingingNobody($"🗑️ Événement #{evt.Id} « {evt.Title} » supprimé.");

    public static string Error(Error error) => Error(error.Message);

    private static string Error(string message) => $"❌ {message}";
}
