namespace AgendaBot.Configuration;

public class BotOptions
{
    public const string SectionName = "Discord";

    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Si renseigné, les commandes sont enregistrées sur ce serveur uniquement (instantané, idéal en dev).
    /// Sinon elles sont enregistrées globalement (propagation jusqu'à 1h).
    /// </summary>
    public ulong? DevGuildId { get; set; }

    /// <summary>Fuseau horaire utilisé pour interpréter les dates saisies par les utilisateurs.</summary>
    public string TimeZone { get; set; } = "Europe/Paris";

    public int ReminderMinutesBefore { get; set; } = 15;
}
