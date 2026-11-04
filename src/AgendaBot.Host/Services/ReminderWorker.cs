using AgendaBot.Application.UseCases;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgendaBot.Services;

public sealed class ReminderWorker(
    DiscordSocketClient client,
    SendDueReminders sendDueReminders,
    ILogger<ReminderWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (client.ConnectionState == ConnectionState.Connected)
                await SendDueRemindersWithoutStoppingOnErrorAsync(stoppingToken);
        }
    }

    private async Task SendDueRemindersWithoutStoppingOnErrorAsync(CancellationToken stoppingToken)
    {
        try
        {
            var report = await sendDueReminders.HandleAsync(stoppingToken);
            LogFailures(report);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Erreur lors de l'envoi des rappels");
        }
    }

    private void LogFailures(RemindersReport report)
    {
        foreach (var failure in report.Failures)
        {
            logger.LogWarning(
                failure.Exception,
                "Rappel non envoyé pour l'événement {EventId}, nouvel essai à la prochaine vérification",
                failure.Event.Id);
        }
    }
}
