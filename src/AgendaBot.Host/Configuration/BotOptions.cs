namespace AgendaBot.Configuration;

public sealed class BotOptions
{
    public const string SectionName = "Discord";

    public string Token { get; set; } = string.Empty;
    public ulong? DevGuildId { get; set; }
    public string TimeZone { get; set; } = "Europe/Paris";

    public int ReminderMinutesBefore { get; set; } = 15;
}
