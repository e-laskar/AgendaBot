using AgendaBot.Application;
using AgendaBot.Application.UseCases;
using AgendaBot.Domain;
using AgendaBot.UnitTests.Application.Fakes;
using AgendaBot.UnitTests.Builders;

namespace AgendaBot.UnitTests.Application;

public class GetEventTests
{
    private static readonly GuildId Guild = AnAgendaEvent.DefaultGuild;

    private readonly InMemoryEventRepository _repository = new();
    private readonly GetEvent _getEvent;

    public GetEventTests() => _getEvent = new GetEvent(_repository);

    [Fact]
    public async Task Returns_event_of_the_guild()
    {
        _repository.Seed(new AnAgendaEvent().Titled("Réunion").InGuild(Guild).BuildPersisted(7));

        var result = await _getEvent.HandleAsync(Guild, 7);

        result.Value.Title.Value.ShouldBe("Réunion");
    }

    [Fact]
    public async Task Fails_when_event_does_not_exist()
    {
        var result = await _getEvent.HandleAsync(Guild, 7);

        result.Error.ShouldBe(ApplicationErrors.EventNotFound(7));
    }

    [Fact]
    public async Task Does_not_reveal_events_of_other_guilds()
    {
        _repository.Seed(new AnAgendaEvent().InGuild(new GuildId(99)).BuildPersisted(7));

        var result = await _getEvent.HandleAsync(Guild, 7);

        result.Error.ShouldBe(ApplicationErrors.EventNotFound(7));
    }
}
