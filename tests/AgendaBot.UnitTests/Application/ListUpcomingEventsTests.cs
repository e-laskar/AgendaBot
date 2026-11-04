using AgendaBot.Application.UseCases;
using AgendaBot.Domain;
using AgendaBot.Testing.Fakes;
using AgendaBot.Testing.Builders;
using Microsoft.Extensions.Time.Testing;

namespace AgendaBot.UnitTests.Application;

public class ListUpcomingEventsTests
{
    private static readonly DateTimeOffset Now = AnAgendaEvent.DefaultNow;
    private static readonly GuildId Guild = AnAgendaEvent.DefaultGuild;

    private readonly InMemoryEventRepository _repository = new();
    private readonly ListUpcomingEvents _listUpcoming;

    public ListUpcomingEventsTests() => _listUpcoming = new ListUpcomingEvents(_repository, new FakeTimeProvider(Now));

    [Fact]
    public async Task Returns_upcoming_events_sorted_by_start()
    {
        _repository.Seed(
            new AnAgendaEvent().Titled("Apéro").StartingAt(Now.AddDays(2)).BuildPersisted(1),
            new AnAgendaEvent().Titled("Réunion").StartingAt(Now.AddDays(1)).BuildPersisted(2));

        var events = await _listUpcoming.HandleAsync(Guild, max: 10);

        events.Select(e => e.Title.Value).ShouldBe(["Réunion", "Apéro"]);
    }

    [Fact]
    public async Task Excludes_past_events()
    {
        _repository.Seed(
            new AnAgendaEvent().Titled("Passé").StartingAt(Now.AddMinutes(-1)).BuildPersisted(1),
            new AnAgendaEvent().Titled("À venir").StartingAt(Now.AddMinutes(1)).BuildPersisted(2));

        var events = await _listUpcoming.HandleAsync(Guild, max: 10);

        events.ShouldHaveSingleItem().Title.Value.ShouldBe("À venir");
    }

    [Fact]
    public async Task Excludes_events_of_other_guilds()
    {
        _repository.Seed(
            new AnAgendaEvent().InGuild(new GuildId(99)).BuildPersisted(1),
            new AnAgendaEvent().InGuild(Guild).BuildPersisted(2));

        var events = await _listUpcoming.HandleAsync(Guild, max: 10);

        events.ShouldHaveSingleItem().Id.ShouldBe(2);
    }

    [Fact]
    public async Task Returns_at_most_the_requested_number_of_events()
    {
        _repository.Seed(Enumerable.Range(1, 5)
            .Select(i => new AnAgendaEvent().StartingAt(Now.AddDays(i)).BuildPersisted(i))
            .ToArray());

        var events = await _listUpcoming.HandleAsync(Guild, max: 3);

        events.Select(e => e.Id).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task Returns_empty_list_when_nothing_is_planned()
    {
        (await _listUpcoming.HandleAsync(Guild, max: 10)).ShouldBeEmpty();
    }
}
