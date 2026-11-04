using AgendaBot.Data;
using AgendaBot.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaBot.Services;

/// <summary>
/// Singleton : un DbContext court est créé par opération via la factory,
/// ce qui évite les problèmes de durée de vie avec les modules Discord.
/// </summary>
public class AgendaService : IAgendaService
{
    private readonly IDbContextFactory<AgendaDbContext> _dbFactory;

    public AgendaService(IDbContextFactory<AgendaDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<AgendaEvent> AddEventAsync(AgendaEvent agendaEvent)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Events.Add(agendaEvent);
        await db.SaveChangesAsync();
        return agendaEvent;
    }

    public async Task<IReadOnlyList<AgendaEvent>> GetUpcomingEventsAsync(ulong guildId, int max = 10)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        return await db.Events
            .Where(e => e.GuildId == guildId && e.StartsAtUtc >= now)
            .OrderBy(e => e.StartsAtUtc)
            .Take(max)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<AgendaEvent?> GetEventAsync(ulong guildId, int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.GuildId == guildId && e.Id == id);
    }

    public async Task<bool> DeleteEventAsync(ulong guildId, int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var deleted = await db.Events
            .Where(e => e.GuildId == guildId && e.Id == id)
            .ExecuteDeleteAsync();
        return deleted > 0;
    }

    public async Task<IReadOnlyList<AgendaEvent>> GetEventsToRemindAsync(DateTime untilUtc)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        return await db.Events
            .Where(e => !e.ReminderSent && e.StartsAtUtc >= now && e.StartsAtUtc <= untilUtc)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task MarkReminderSentAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Events
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ReminderSent, true));
    }
}
