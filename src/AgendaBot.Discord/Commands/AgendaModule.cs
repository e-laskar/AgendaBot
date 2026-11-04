using AgendaBot.Application.UseCases;
using AgendaBot.Discord.Presentation;
using AgendaBot.Domain;
using Discord;
using Discord.Interactions;

namespace AgendaBot.Discord.Commands;

[Group("agenda", "Gestion de l'agenda du serveur")]
[CommandContextType(InteractionContextType.Guild)]
public sealed class AgendaModule(
    AddEvent addEvent,
    ListUpcomingEvents listUpcomingEvents,
    GetEvent getEvent,
    DeleteEvent deleteEvent,
    UserDateParser userDateParser) : InteractionModuleBase<SocketInteractionContext>
{
    private GuildId CurrentGuild => new(Context.Guild.Id);

    [SlashCommand("ajouter", "Ajoute un événement à l'agenda")]
    public async Task AddAsync(
        [Summary("titre", "Titre de l'événement"), MaxLength(EventTitle.MaxLength)] string title,
        [Summary("date", "Date et heure, format jj/mm/aaaa hh:mm")] string date,
        [Summary("description", "Description optionnelle"), MaxLength(EventDescription.MaxLength)] string? description = null,
        [Summary("duree", "Durée en minutes"), MinValue(1)] int? durationMinutes = null)
    {
        if (!userDateParser.TryParse(date, out var startsAt))
        {
            await RespondWithErrorAsync($"Date invalide. Format attendu : `{UserDateParser.Format}` (ex. `{UserDateParser.Example}`).");
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

    private Task RespondWithErrorAsync(Error error) => RespondWithErrorAsync(error.Message);

    private Task RespondWithErrorAsync(string message) => RespondAsync($"❌ {message}", ephemeral: true);
}
