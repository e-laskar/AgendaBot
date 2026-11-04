using AgendaBot.Configuration;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgendaBot.Services;

/// <summary>
/// Vérifie chaque minute les événements à venir et poste un rappel dans le salon d'origine.
/// </summary>
public class ReminderService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly DiscordSocketClient _client;
    private readonly IAgendaService _agenda;
    private readonly BotOptions _options;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        DiscordSocketClient client,
        IAgendaService agenda,
        IOptions<BotOptions> options,
        ILogger<ReminderService> logger)
    {
        _client = client;
        _agenda = agenda;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (_client.ConnectionState != ConnectionState.Connected)
                continue;

            try
            {
                await SendRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi des rappels");
            }
        }
    }

    private async Task SendRemindersAsync()
    {
        var until = DateTime.UtcNow.AddMinutes(_options.ReminderMinutesBefore);
        var events = await _agenda.GetEventsToRemindAsync(until);

        foreach (var evt in events)
        {
            if (_client.GetChannel(evt.ChannelId) is IMessageChannel channel)
            {
                var start = new DateTimeOffset(evt.StartsAtUtc).ToUnixTimeSeconds();
                await channel.SendMessageAsync(
                    $"⏰ Rappel : **{evt.Title}** commence <t:{start}:R> (<t:{start}:t>).");
            }
            else
            {
                _logger.LogWarning("Salon {ChannelId} introuvable pour l'événement {EventId}", evt.ChannelId, evt.Id);
            }

            await _agenda.MarkReminderSentAsync(evt.Id);
        }
    }
}
