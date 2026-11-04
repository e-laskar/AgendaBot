using AgendaBot.Application.Ports;
using AgendaBot.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgendaBot.Persistence;

public sealed class EfEventRepository(IDbContextFactory<AgendaDbContext> dbFactory) : IEventRepository
{
    public async Task<AgendaEvent> AddAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var record = evt.ToRecord();
        db.Events.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return record.ToDomain();
    }

    public async Task<AgendaEvent?> GetAsync(GuildId guildId, int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var record = await db.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.GuildId == guildId.Value && e.Id == id, cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<AgendaEvent>> GetUpcomingByStartDateAsync(
        GuildId guildId, DateTimeOffset from, int max, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var fromUtc = from.UtcDateTime;
        var records = await db.Events.AsNoTracking()
            .Where(e => e.GuildId == guildId.Value && e.StartsAtUtc >= fromUtc)
            .OrderBy(e => e.StartsAtUtc)
            .Take(max)
            .ToListAsync(cancellationToken);
        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<AgendaEvent>> GetWithoutReminderStartingBetweenAsync(
        DateTimeOffset after, DateTimeOffset until, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var afterUtc = after.UtcDateTime;
        var untilUtc = until.UtcDateTime;
        var records = await db.Events.AsNoTracking()
            .Where(e => !e.ReminderSent && e.StartsAtUtc > afterUtc && e.StartsAtUtc <= untilUtc)
            .ToListAsync(cancellationToken);
        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<bool> TryClaimReminderAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var claimedRows = await db.Events
            .Where(e => e.Id == evt.Id && !e.ReminderSent)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ReminderSent, true), cancellationToken);
        return claimedRows == 1;
    }

    public async Task ReleaseReminderClaimAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Events
            .Where(e => e.Id == evt.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ReminderSent, false), cancellationToken);
    }

    public async Task DeleteAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Events
            .Where(e => e.Id == evt.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
