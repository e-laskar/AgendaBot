using AgendaBot.Application.UseCases;
using AgendaBot.Domain;
using AgendaBot.Testing.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace AgendaBot.UnitTests.Application;

public class AddEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryEventRepository _repository = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly AddEvent _addEvent;

    public AddEventTests() => _addEvent = new AddEvent(_repository, _time);

    private static AddEventCommand ValidCommand() => new(
        new GuildId(1), new ChannelId(2), new UserId(3),
        Title: "Réunion", Description: "Salle 2",
        StartsAt: Now.AddDays(1), Duration: TimeSpan.FromMinutes(30));

    [Fact]
    public async Task Stores_valid_event()
    {
        var command = ValidCommand();

        await _addEvent.HandleAsync(command);

        var stored = _repository.All.ShouldHaveSingleItem();
        stored.ShouldSatisfyAllConditions(
            () => stored.GuildId.ShouldBe(command.GuildId),
            () => stored.ChannelId.ShouldBe(command.ChannelId),
            () => stored.CreatedBy.ShouldBe(command.CreatedBy),
            () => stored.Title.Value.ShouldBe("Réunion"),
            () => stored.Description!.Value.ShouldBe("Salle 2"),
            () => stored.StartsAt.ShouldBe(command.StartsAt),
            () => stored.Duration.ShouldBe(TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public async Task Returns_stored_event_with_its_id()
    {
        var result = await _addEvent.HandleAsync(ValidCommand());

        result.Value.Id.ShouldBe(_repository.All.Single().Id);
        result.Value.Id.ShouldNotBe(0);
    }

    [Fact]
    public async Task Rejects_invalid_title()
    {
        var result = await _addEvent.HandleAsync(ValidCommand() with { Title = "  " });

        result.Error.ShouldBe(DomainErrors.TitleRequired);
        _repository.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rejects_invalid_description()
    {
        var tooLong = new string('a', EventDescription.MaxLength + 1);

        var result = await _addEvent.HandleAsync(ValidCommand() with { Description = tooLong });

        result.Error.ShouldBe(DomainErrors.DescriptionTooLong);
        _repository.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rejects_event_starting_before_current_time()
    {
        var command = ValidCommand() with { StartsAt = Now.AddMinutes(10) };
        _time.Advance(TimeSpan.FromMinutes(11));

        var result = await _addEvent.HandleAsync(command);

        result.Error.ShouldBe(DomainErrors.StartInPast);
        _repository.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accepts_event_without_description_nor_duration()
    {
        var result = await _addEvent.HandleAsync(ValidCommand() with { Description = null, Duration = null });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Description.ShouldBeNull();
        result.Value.Duration.ShouldBeNull();
    }
}
