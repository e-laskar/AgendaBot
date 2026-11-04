using AgendaBot;
using AgendaBot.Configuration;
using AgendaBot.Discord;
using AgendaBot.Persistence;
using Microsoft.Extensions.Hosting;

DotEnv.Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddDiscordBot(builder.Configuration)
    .AddAgendaDatabase(builder.Configuration)
    .AddAgendaUseCases();

var host = builder.Build();

host.Services.FailFastOnInvalidBotOptions();
await host.Services.MigrateAgendaDatabaseAsync();

await host.RunAsync();
