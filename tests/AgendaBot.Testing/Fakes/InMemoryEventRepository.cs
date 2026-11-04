using AgendaBot.Application.Ports;
using AgendaBot.Domain;

namespace AgendaBot.Testing.Fakes;

public sealed class InMemoryEventRepository : IEventRepository
{
    private readonly Dictionary<int, AgendaEvent> _events = [];
    private readonly HashSet<int> _claimedByAnotherInstanceIds = [];
    private readonly HashSet<int> _failingClaimIds = [];
    private int _nextId = 1;

    public IReadOnlyCollection<AgendaEvent> All => _events.Values.Select(Copy).ToList();

    public void Seed(params AgendaEvent[] events)
    {
        foreach (var evt in events)
        {
            _events[evt.Id] = Copy(evt);
            _nextId = Math.Max(_nextId, evt.Id + 1);
        }
    }

    public Task<AgendaEvent> AddAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        var saved = CopyWithId(evt, _nextId++);
        _events[saved.Id] = saved;
        return Task.FromResult(Copy(saved));
    }

    public Task<AgendaEvent?> GetAsync(GuildId guildId, int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_events.TryGetValue(id, out var evt) && evt.GuildId == guildId ? Copy(evt) : null);

    public Task<IReadOnlyList<AgendaEvent>> GetUpcomingByStartDateAsync(
        GuildId guildId, DateTimeOffset from, int max, CancellationToken cancellationToken = default) =>
        Result(_events.Values
            .Where(e => e.GuildId == guildId && e.StartsAt >= from)
            .OrderBy(e => e.StartsAt)
            .Take(max));

    public Task<int> CountUpcomingCreatedByAsync(
        GuildId guildId, UserId creator, DateTimeOffset from, CancellationToken cancellationToken = default) =>
        Task.FromResult(_events.Values.Count(e => e.GuildId == guildId && e.CreatedBy == creator && e.StartsAt >= from));

    public Task<IReadOnlyList<AgendaEvent>> GetWithoutReminderStartingBetweenAsync(
        DateTimeOffset after, DateTimeOffset until, CancellationToken cancellationToken = default) =>
        Result(_events.Values.Where(e => !e.ReminderSent && e.StartsAt > after && e.StartsAt <= until));

    public Task<bool> TryClaimReminderAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        if (_failingClaimIds.Contains(evt.Id))
            throw new InvalidOperationException($"Réservation impossible pour l'événement {evt.Id}.");
        if (_claimedByAnotherInstanceIds.Remove(evt.Id))
            ClaimStored(evt.Id);

        if (!_events.TryGetValue(evt.Id, out var stored) || stored.ReminderSent)
            return Task.FromResult(false);

        ClaimStored(evt.Id);
        return Task.FromResult(true);
    }

    public Task ReleaseReminderClaimAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        if (_events.TryGetValue(evt.Id, out var stored))
            _events[evt.Id] = CopyWithReminderSent(stored, reminderSent: false);
        return Task.CompletedTask;
    }

    public void AnotherInstanceClaimsBeforeUs(int eventId) => _claimedByAnotherInstanceIds.Add(eventId);

    public void FailClaimsFor(int eventId) => _failingClaimIds.Add(eventId);

    private void ClaimStored(int eventId) =>
        _events[eventId] = CopyWithReminderSent(_events[eventId], reminderSent: true);

    public Task DeleteAsync(AgendaEvent evt, CancellationToken cancellationToken = default)
    {
        _events.Remove(evt.Id);
        return Task.CompletedTask;
    }

    private static Task<IReadOnlyList<AgendaEvent>> Result(IEnumerable<AgendaEvent> events) =>
        Task.FromResult<IReadOnlyList<AgendaEvent>>(events.Select(Copy).ToList());

    private static AgendaEvent Copy(AgendaEvent evt) => CopyWithId(evt, evt.Id);

    private static AgendaEvent CopyWithId(AgendaEvent evt, int id) =>
        AgendaEvent.RestoreFromPersistence(
            id, evt.GuildId, evt.ChannelId, evt.CreatedBy, evt.Title, evt.Description,
            evt.StartsAt, evt.Duration, evt.ReminderSent);

    private static AgendaEvent CopyWithReminderSent(AgendaEvent evt, bool reminderSent) =>
        AgendaEvent.RestoreFromPersistence(
            evt.Id, evt.GuildId, evt.ChannelId, evt.CreatedBy, evt.Title, evt.Description,
            evt.StartsAt, evt.Duration, reminderSent);
}
