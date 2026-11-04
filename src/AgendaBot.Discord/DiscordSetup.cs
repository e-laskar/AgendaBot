using AgendaBot.Application.Ports;
using AgendaBot.Discord.Reminders;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgendaBot.Discord;

public static class DiscordSetup
{
    public static IServiceCollection AddDiscordBot(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedBotOptions(configuration);
        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds,
            LogLevel = LogSeverity.Info
        }));
        services.AddSingleton(sp => new InteractionService(
            sp.GetRequiredService<DiscordSocketClient>(),
            new InteractionServiceConfig { LogLevel = LogSeverity.Info }));
        services.AddSingleton<IReminderNotifier, DiscordReminderNotifier>();
        services.AddHostedService<DiscordBotService>();
        services.AddHostedService<ReminderWorker>();
        return services;
    }

    public static void FailFastOnInvalidBotOptions(this IServiceProvider services) =>
        _ = services.GetRequiredService<IOptions<BotOptions>>().Value;

    private static void AddValidatedBotOptions(this IServiceCollection services, IConfiguration configuration) =>
        services.AddOptions<BotOptions>()
            .Bind(configuration.GetSection(BotOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Token),
                "Token Discord manquant : définir Discord__Token (variable d'environnement / .env) ou Discord:Token (user-secrets).")
            .Validate(o => o.ReminderMinutesBefore > 0,
                "Discord__ReminderMinutesBefore doit être strictement positif.")
            .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.TimeZone, out _),
                "Discord__TimeZone n'est pas un fuseau horaire reconnu (ex. Europe/Paris).")
            .ValidateOnStart();
}
