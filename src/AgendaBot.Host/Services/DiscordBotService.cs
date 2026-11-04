using System.Reflection;
using AgendaBot.Configuration;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgendaBot.Services;

public sealed class DiscordBotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly BotOptions _options;
    private readonly ILogger<DiscordBotService> _logger;

    public DiscordBotService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        IOptions<BotOptions> options,
        ILogger<DiscordBotService> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += LogAsync;
        _interactions.Log += LogAsync;
        _client.Ready += RegisterCommandsAsync;
        _client.InteractionCreated += HandleInteractionAsync;
        _interactions.SlashCommandExecuted += OnSlashCommandExecutedAsync;

        await _interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

        await _client.LoginAsync(TokenType.Bot, _options.Token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private async Task RegisterCommandsAsync()
    {
        if (_options.DevGuildId is { } guildId)
        {
            await _interactions.RegisterCommandsToGuildAsync(guildId);
            _logger.LogInformation("Commandes enregistrées sur le serveur {GuildId}", guildId);
        }
        else
        {
            await _interactions.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Commandes enregistrées globalement");
        }
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(_client, interaction);
        await _interactions.ExecuteCommandAsync(context, _services);
    }

    private async Task OnSlashCommandExecutedAsync(SlashCommandInfo command, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess)
            return;

        _logger.LogWarning("Échec de /{Command} : {Error} {Reason}", command?.Name, result.Error, result.ErrorReason);

        var message = result.Error == InteractionCommandError.UnmetPrecondition
            ? $"⛔ {result.ErrorReason}"
            : "❌ Une erreur est survenue lors de l'exécution de la commande.";

        if (context.Interaction.HasResponded)
            await context.Interaction.FollowupAsync(message, ephemeral: true);
        else
            await context.Interaction.RespondAsync(message, ephemeral: true);
    }

    private Task LogAsync(LogMessage msg)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            _ => LogLevel.Trace
        };
        _logger.Log(level, msg.Exception, "[{Source}] {Message}", msg.Source, msg.Message);
        return Task.CompletedTask;
    }
}
