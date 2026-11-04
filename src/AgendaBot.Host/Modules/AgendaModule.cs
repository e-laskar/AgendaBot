using System.Globalization;
using AgendaBot.Application.UseCases;
using AgendaBot.Configuration;
using AgendaBot.Domain;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;

namespace AgendaBot.Modules;

[Group("agenda", "Gestion de l'agenda du serveur")]
[CommandContextType(InteractionContextType.Guild)]
public sealed class AgendaModule(
    AddEvent addEvent,
    ListUpcomingEvents listUpcomingEvents,
    GetEvent getEvent,
    DeleteEvent deleteEvent,
    IOptions<BotOptions> options) : InteractionModuleBase<SocketInteractionContext>
{
    private const string UserDateFormat = "dd/MM/yyyy HH:mm";
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private readonly TimeZoneInfo _userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    private GuildId CurrentGuild => new(Context.Guild.Id);

    [SlashCommand("ajouter", "Ajoute un événement à l'agenda")]
    public async Task AddAsync(
        [Summary("titre", "Titre de l'événement"), MaxLength(EventTitle.MaxLength)] string title,
        [Summary("date", "Date et heure, format jj/mm/aaaa hh:mm")] string date,
        [Summary("description", "Description optionnelle"), MaxLength(EventDescription.MaxLength)] string? description = null,
        [Summary("duree", "Durée en minutes"), MinValue(1)] int? durationMinutes = null)
    {
        if (!TryParseUserDate(date, out var startsAt))
        {
            await RespondWithErrorAsync($"Date invalide. Format attendu : `{UserDateFormat}` (ex. `24/12/2026 20:00`).");
            return;
        }

        var result = await addEvent.HandleAsync(new AddEventCommand(
            CurrentGuild,
            new ChannelId(Context.Channel.Id),
            new UserId(Context.User.Id),
            title,
            description,
            startsAt,
            durationMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : null));

        if (!result.IsSuccess)
        {
            await RespondWithErrorAsync(result.Error);
            return;
        }

        await RespondAsync("✅ Événement ajouté !", embed: AgendaEventEmbeds.Details(result.Value));
    }

    [SlashCommand("lister", "Affiche les prochains événements")]
    public async Task ListAsync(
        [Summary("nombre", "Nombre d'événements à afficher"), MinValue(1), MaxValue(25)] int count = 10)
    {
        var events = await listUpcomingEvents.HandleAsync(CurrentGuild, count);
        if (events.Count == 0)
        {
            await RespondAsync("📭 Aucun événement à venir.", ephemeral: true);
            return;
        }

        await RespondAsync(embed: AgendaEventEmbeds.UpcomingList(events));
    }

    [SlashCommand("voir", "Affiche le détail d'un événement")]
    public async Task ShowAsync([Summary("id", "Identifiant de l'événement")] int id)
    {
        var result = await getEvent.HandleAsync(CurrentGuild, id);
        if (!result.IsSuccess)
        {
            await RespondWithErrorAsync(result.Error);
            return;
        }

        await RespondAsync(embed: AgendaEventEmbeds.Details(result.Value));
    }

    [SlashCommand("supprimer", "Supprime un événement")]
    [RequireUserPermission(GuildPermission.ManageEvents)]
    public async Task DeleteAsync([Summary("id", "Identifiant de l'événement")] int id)
    {
        var result = await deleteEvent.HandleAsync(CurrentGuild, id);
        if (!result.IsSuccess)
        {
            await RespondWithErrorAsync(result.Error);
            return;
        }

        await RespondAsync($"🗑️ Événement #{id} « {result.Value.Title} » supprimé.");
    }

    private bool TryParseUserDate(string text, out DateTimeOffset startsAt)
    {
        startsAt = default;
        if (!DateTime.TryParseExact(text, UserDateFormat, French, DateTimeStyles.None, out var parsed))
            return false;

        var localDate = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
        if (IsSkippedByDaylightSavingTime(localDate))
            return false;

        startsAt = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localDate, _userTimeZone));
        return true;
    }

    private bool IsSkippedByDaylightSavingTime(DateTime localDate) => _userTimeZone.IsInvalidTime(localDate);

    private Task RespondWithErrorAsync(Error error) => RespondWithErrorAsync(error.Message);

    private Task RespondWithErrorAsync(string message) => RespondAsync($"❌ {message}", ephemeral: true);
}
