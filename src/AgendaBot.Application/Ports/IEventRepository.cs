using AgendaBot.Domain;

namespace AgendaBot.Application.Ports;

/// <summary>Port sortant : stockage des événements.</summary>
public interface IEventRepository
{
    /// <summary>Enregistre un nouvel événement et le renvoie avec son <see cref="AgendaEvent.Id"/> attribué.</summary>
    Task<AgendaEvent> AddAsync(AgendaEvent evt, CancellationToken cancellationToken = default);

    Task<AgendaEvent?> GetAsync(GuildId guildId, int id, CancellationToken cancellationToken = default);

    /// <summary>Événements du serveur commençant à partir de <paramref name="from"/>, triés par date de début.</summary>
    Task<IReadOnlyList<AgendaEvent>> GetUpcomingAsync(
        GuildId guildId, DateTimeOffset from, int max, CancellationToken cancellationToken = default);

    /// <summary>Événements (tous serveurs) sans rappel envoyé, commençant dans ]<paramref name="after"/>, <paramref name="until"/>].</summary>
    Task<IReadOnlyList<AgendaEvent>> GetWithoutReminderStartingBetweenAsync(
        DateTimeOffset after, DateTimeOffset until, CancellationToken cancellationToken = default);

    /// <summary>Enregistre l'état modifiable d'un événement existant (rappel envoyé).</summary>
    Task UpdateAsync(AgendaEvent evt, CancellationToken cancellationToken = default);

    Task DeleteAsync(AgendaEvent evt, CancellationToken cancellationToken = default);
}
