using AgendaBot.Application.Ports;
using AgendaBot.Application.UseCases;
using AgendaBot.Configuration;
using AgendaBot.Persistence;
using AgendaBot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgendaBot;

internal static class AgendaBotSetup
{
    public static IServiceCollection AddValidatedBotOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BotOptions>()
            .Bind(configuration.GetSection(BotOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Token),
                "Token Discord manquant : définir Discord__Token (variable d'environnement / .env) ou Discord:Token (user-secrets).")
            .Validate(o => o.ReminderMinutesBefore > 0,
                "Discord__ReminderMinutesBefore doit être strictement positif.")
            .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.TimeZone, out _),
                "Discord__TimeZone n'est pas un fuseau horaire reconnu (ex. Europe/Paris).")
            .ValidateOnStart();
        return services;
    }

    public static IServiceCollection AddAgendaDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Agenda");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Chaîne de connexion manquante : définir ConnectionStrings__Agenda.");

        var provider = configuration["Database:Provider"] ?? "Sqlite";
        return services.AddAgendaPersistence(provider, connectionString);
    }

    public static IServiceCollection AddAgendaUseCases(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(sp => new ReminderSettings(
            TimeSpan.FromMinutes(sp.GetRequiredService<IOptions<BotOptions>>().Value.ReminderMinutesBefore)));
        services.AddSingleton<AddEvent>();
        services.AddSingleton<ListUpcomingEvents>();
        services.AddSingleton<GetEvent>();
        services.AddSingleton<DeleteEvent>();
        services.AddSingleton<SendDueReminders>();
        return services;
    }

    public static IServiceCollection AddDiscordBot(this IServiceCollection services)
    {
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
}
