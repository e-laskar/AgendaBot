using System.Globalization;
using AgendaBot.Configuration;
using AgendaBot.Models;
using AgendaBot.Services;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;

namespace AgendaBot.Modules;

[Group("agenda", "Gestion de l'agenda du serveur")]
[CommandContextType(InteractionContextType.Guild)]
public class AgendaModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string DateFormat = "dd/MM/yyyy HH:mm";
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private readonly IAgendaService _agenda;
    private readonly TimeZoneInfo _timeZone;

    public AgendaModule(IAgendaService agenda, IOptions<BotOptions> options)
    {
        _agenda = agenda;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
    }

    [SlashCommand("ajouter", "Ajoute un événement à l'agenda")]
    public async Task AddAsync(
        [Summary("titre", "Titre de l'événement"), MaxLength(100)] string title,
        [Summary("date", "Date et heure, format jj/mm/aaaa hh:mm")] string date,
        [Summary("description", "Description optionnelle"), MaxLength(1000)] string? description = null,
        [Summary("duree", "Durée en minutes"), MinValue(1)] int? durationMinutes = null)
    {
        if (!DateTime.TryParseExact(date, DateFormat, French, DateTimeStyles.None, out var localDate))
        {
            await RespondAsync($"❌ Date invalide. Format attendu : `{DateFormat}` (ex. `24/12/2026 20:00`).", ephemeral: true);
            return;
        }

        var startsAtUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified), _timeZone);
        if (startsAtUtc <= DateTime.UtcNow)
        {
            await RespondAsync("❌ La date doit être dans le futur.", ephemeral: true);
            return;
        }

        var evt = await _agenda.AddEventAsync(new AgendaEvent
        {
            GuildId = Context.Guild.Id,
            ChannelId = Context.Channel.Id,
            CreatedByUserId = Context.User.Id,
            Title = title,
            Description = description,
            StartsAtUtc = startsAtUtc,
            DurationMinutes = durationMinutes
        });

        await RespondAsync("✅ Événement ajouté !", embed: BuildEventEmbed(evt));
    }

    [SlashCommand("lister", "Affiche les prochains événements")]
    public async Task ListAsync(
        [Summary("nombre", "Nombre d'événements à afficher"), MinValue(1), MaxValue(25)] int count = 10)
    {
        var events = await _agenda.GetUpcomingEventsAsync(Context.Guild.Id, count);
        if (events.Count == 0)
        {
            await RespondAsync("📭 Aucun événement à venir.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("📅 Prochains événements")
            .WithColor(Color.Blue);

        foreach (var evt in events)
        {
            var start = ToUnix(evt.StartsAtUtc);
            embed.AddField($"#{evt.Id} · {evt.Title}", $"<t:{start}:F> (<t:{start}:R>)");
        }

        await RespondAsync(embed: embed.Build());
    }

    [SlashCommand("voir", "Affiche le détail d'un événement")]
    public async Task ShowAsync([Summary("id", "Identifiant de l'événement")] int id)
    {
        var evt = await _agenda.GetEventAsync(Context.Guild.Id, id);
        if (evt is null)
        {
            await RespondAsync($"❌ Événement #{id} introuvable.", ephemeral: true);
            return;
        }

        await RespondAsync(embed: BuildEventEmbed(evt));
    }

    [SlashCommand("supprimer", "Supprime un événement")]
    [RequireUserPermission(GuildPermission.ManageEvents)]
    public async Task DeleteAsync([Summary("id", "Identifiant de l'événement")] int id)
    {
        var deleted = await _agenda.DeleteEventAsync(Context.Guild.Id, id);
        await RespondAsync(
            deleted ? $"🗑️ Événement #{id} supprimé." : $"❌ Événement #{id} introuvable.",
            ephemeral: !deleted);
    }

    private static Embed BuildEventEmbed(AgendaEvent evt)
    {
        var start = ToUnix(evt.StartsAtUtc);
        var embed = new EmbedBuilder()
            .WithTitle($"📌 {evt.Title}")
            .WithDescription(evt.Description ?? "_Pas de description_")
            .WithColor(Color.Green)
            .AddField("Début", $"<t:{start}:F>\n<t:{start}:R>", inline: true)
            .AddField("Créé par", MentionUtils.MentionUser(evt.CreatedByUserId), inline: true)
            .WithFooter($"ID : {evt.Id}");

        if (evt.DurationMinutes is { } duration)
            embed.AddField("Durée", TimeSpan.FromMinutes(duration).ToString(@"hh\hmm"), inline: true);

        return embed.Build();
    }

    // Les timestamps Discord <t:...> s'affichent automatiquement dans le fuseau de chaque utilisateur.
    private static long ToUnix(DateTime utc) => new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeSeconds();
}
