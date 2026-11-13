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
        [Summary("date", "jj/mm/aaaa hh:mm, ou jj/mm/aaaa pour toute la journée")] string date,
        [Summary("description", "Description optionnelle"), MaxLength(EventDescription.MaxLength)] string? description = null,
        [Summary("duree", "Durée en minutes"), MinValue(1)] int? durationMinutes = null)
    {
        if (!userDateParser.TryParse(date, out var eventDate))
        {
            await RespondAsync(AgendaEventMessages.InvalidDate, ephemeral: true);
            return;
        }

        await DeferAsync();
        var result = await addEvent.HandleAsync(new AddEventCommand(
            CurrentGuild,
            new ChannelId(Context.Channel.Id),
            new UserId(Context.User.Id),
            title,
            description,
            eventDate,
            durationMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : null));

        if (!result.IsSuccess)
        {
            await CompleteWithErrorAsync(result.Error);
            return;
        }

        await CompletePubliclyAsync(
            OutgoingMessage.PingingNobody(AgendaEventMessages.Added),
            AgendaEventEmbeds.Details(result.Value));
    }

    [SlashCommand("lister", "Affiche les prochains événements")]
    public async Task ListAsync(
        [Summary("nombre", "Nombre d'événements à afficher"), MinValue(1), MaxValue(25)] int count = 10)
    {
        await DeferAsync();
        var events = await listUpcomingEvents.HandleAsync(CurrentGuild, count);
        if (events.Count == 0)
        {
            await CompletePrivatelyAsync(AgendaEventMessages.NoUpcomingEvents);
            return;
        }

        await CompletePubliclyAsync(embed: AgendaEventEmbeds.UpcomingList(events));
    }

    [SlashCommand("voir", "Affiche le détail d'un événement")]
    public async Task ShowAsync([Summary("id", "Identifiant de l'événement")] int id)
    {
        await DeferAsync();
        var result = await getEvent.HandleAsync(CurrentGuild, id);
        if (!result.IsSuccess)
        {
            await CompleteWithErrorAsync(result.Error);
            return;
        }

        await CompletePubliclyAsync(embed: AgendaEventEmbeds.Details(result.Value));
    }

    [SlashCommand("supprimer", "Supprime un événement")]
    [RequireUserPermission(GuildPermission.ManageEvents)]
    public async Task DeleteAsync([Summary("id", "Identifiant de l'événement")] int id)
    {
        await DeferAsync();
        var result = await deleteEvent.HandleAsync(CurrentGuild, id);
        if (!result.IsSuccess)
        {
            await CompleteWithErrorAsync(result.Error);
            return;
        }

        await CompletePubliclyAsync(AgendaEventMessages.Deleted(result.Value));
    }

    private Task CompletePubliclyAsync(OutgoingMessage? message = null, Embed? embed = null) =>
        ModifyOriginalResponseAsync(response =>
        {
            if (message is not null)
            {
                response.Content = message.Text;
                response.AllowedMentions = message.AllowedMentions;
            }
            if (embed is not null)
                response.Embed = embed;
        });

    private Task CompleteWithErrorAsync(Error error) => CompletePrivatelyAsync(AgendaEventMessages.Error(error));

    private Task CompletePrivatelyAsync(string message) =>
        Context.Interaction.ReplacePublicDeferralWithPrivateMessageAsync(message);
}
