using AgendaBot.Application.UseCases;
using AgendaBot.Discord;
using AgendaBot.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgendaBot;

internal static class AgendaBotSetup
{
    public static IServiceCollection AddAgendaDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Agenda");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Chaîne de connexion manquante : définir ConnectionStrings__Agenda.");

        return services.AddAgendaPersistence(connectionString);
    }

    public static IServiceCollection AddAgendaUseCases(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedAgendaOptions(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(sp => new CreationQuota(
            sp.GetRequiredService<IOptions<AgendaOptions>>().Value.MaxUpcomingEventsPerCreator));
        services.AddSingleton(sp => new ReminderSettings(
            TimeSpan.FromMinutes(sp.GetRequiredService<IOptions<BotOptions>>().Value.ReminderMinutesBefore)));
        services.AddSingleton<AddEvent>();
        services.AddSingleton<ListUpcomingEvents>();
        services.AddSingleton<GetEvent>();
        services.AddSingleton<EditEvent>();
        services.AddSingleton<DeleteEvent>();
        services.AddSingleton<SendDueReminders>();
        return services;
    }

    private static void AddValidatedAgendaOptions(this IServiceCollection services, IConfiguration configuration) =>
        services.AddOptions<AgendaOptions>()
            .Bind(configuration.GetSection(AgendaOptions.SectionName))
            .Validate(o => o.MaxUpcomingEventsPerCreator > 0,
                "Agenda__MaxUpcomingEventsPerCreator doit être strictement positif.")
            .ValidateOnStart();
}
