using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AgendaBot.Configuration;
using AgendaBot.Data;
using AgendaBot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// Charge un éventuel fichier .env avant la création du host, pour que DOTNET_ENVIRONMENT
// et les variables Discord__* / ConnectionStrings__* soient pris en compte.
// Ordre de priorité (du plus faible au plus fort) : appsettings.json, appsettings.{Env}.json,
// user-secrets (Development), variables d'environnement (.env inclus), arguments de ligne de commande.
DotEnv.Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<BotOptions>()
    .Bind(builder.Configuration.GetSection(BotOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Token),
        "Token Discord manquant : définir Discord__Token (variable d'environnement / .env) ou Discord:Token (user-secrets).")
    .Validate(o => o.ReminderMinutesBefore > 0,
        "Discord__ReminderMinutesBefore doit être strictement positif.")
    .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.TimeZone, out _),
        "Discord__TimeZone n'est pas un fuseau horaire reconnu (ex. Europe/Paris).")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("Agenda");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Chaîne de connexion manquante : définir ConnectionStrings__Agenda.");

// Database__Provider : "Sqlite" (défaut, dev local) ou "Postgres" (docker compose / production).
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
builder.Services.AddDbContextFactory<AgendaDbContext>(options =>
{
    switch (provider.ToLowerInvariant())
    {
        case "sqlite":
            options.UseSqlite(connectionString);
            break;
        case "postgres" or "postgresql" or "npgsql":
            // Retry : tolère une base pas encore prête ou une coupure réseau passagère.
            options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure());
            break;
        default:
            throw new InvalidOperationException(
                $"Database__Provider inconnu : « {provider} » (valeurs possibles : Sqlite, Postgres).");
    }
});

builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds,
    LogLevel = LogSeverity.Info
}));
builder.Services.AddSingleton(sp => new InteractionService(
    sp.GetRequiredService<DiscordSocketClient>(),
    new InteractionServiceConfig { LogLevel = LogSeverity.Info }));

builder.Services.AddSingleton<IAgendaService, AgendaService>();
builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddHostedService<ReminderService>();

var host = builder.Build();

// Valide la configuration avant toute autre opération (accès à .Value => exécute les Validate ci-dessus).
_ = host.Services.GetRequiredService<IOptions<BotOptions>>().Value;

// Squelette : création de la base au démarrage. Passer aux migrations EF Core pour faire évoluer le schéma.
await using (var db = await host.Services.GetRequiredService<IDbContextFactory<AgendaDbContext>>().CreateDbContextAsync())
{
    await db.Database.EnsureCreatedAsync();
}

await host.RunAsync();
