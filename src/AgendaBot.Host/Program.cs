using AgendaBot;
using AgendaBot.Configuration;
using AgendaBot.Persistence;
using Microsoft.Extensions.Hosting;

DotEnv.Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddValidatedBotOptions(builder.Configuration)
    .AddAgendaDatabase(builder.Configuration)
    .AddAgendaUseCases()
    .AddDiscordBot();

var host = builder.Build();

host.Services.FailFastOnInvalidBotOptions();
await host.Services.EnsureAgendaDatabaseCreatedAsync();

await host.RunAsync();
