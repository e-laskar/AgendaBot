using AgendaBot.Application;
using AgendaBot.Application.UseCases;
using AgendaBot.Domain;
using AgendaBot.UnitTests.Application.Fakes;
using AgendaBot.UnitTests.Builders;

namespace AgendaBot.UnitTests.Application;

public class DeleteEventTests
{
    private static readonly GuildId Guild = AnAgendaEvent.DefaultGuild;

    private readonly InMemoryEventRepository _repository = new();
    private readonly DeleteEvent _deleteEvent;

    public DeleteEventTests() => _deleteEvent = new DeleteEvent(_repository);

    [Fact]
    public async Task Deletes_event_and_returns_it()
    {
        _repository.Seed(
            new AnAgendaEvent().Titled("Réunion").InGuild(Guild).BuildPersisted(7),
            new AnAgendaEvent().InGuild(Guild).BuildPersisted(8));

        var result = await _deleteEvent.HandleAsync(Guild, 7);

        result.Value.Title.Value.ShouldBe("Réunion");
        _repository.All.ShouldHaveSingleItem().Id.ShouldBe(8);
    }

    [Fact]
    public async Task Fails_when_event_does_not_exist()
    {
        var result = await _deleteEvent.HandleAsync(Guild, 7);

        result.Error.ShouldBe(ApplicationErrors.EventNotFound(7));
    }

    [Fact]
    public async Task Cannot_delete_event_of_another_guild()
    {
        _repository.Seed(new AnAgendaEvent().InGuild(new GuildId(99)).BuildPersisted(7));

        var result = await _deleteEvent.HandleAsync(Guild, 7);

        result.Error.ShouldBe(ApplicationErrors.EventNotFound(7));
        _repository.All.ShouldHaveSingleItem();
    }
}
