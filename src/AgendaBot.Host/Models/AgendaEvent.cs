namespace AgendaBot.Models;

public class AgendaEvent
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong CreatedByUserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime StartsAtUtc { get; set; }
    public int? DurationMinutes { get; set; }

    public bool ReminderSent { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
