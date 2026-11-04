using AgendaBot;
using AgendaBot.Configuration;
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
await host.Services.EnsureDatabaseCreatedAsync();

await host.RunAsync();
