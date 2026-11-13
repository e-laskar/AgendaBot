using AgendaBot.Application;
using AgendaBot.Application.UseCases;
using AgendaBot.Domain;
using AgendaBot.Testing.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace AgendaBot.UnitTests.Application;

public class AddEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TodayStartsAt = new(2026, 11, 3, 23, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TodayEndsAt = TodayStartsAt.AddDays(1);
    private const int MaxUpcomingEventsPerCreator = 2;

    private readonly InMemoryEventRepository _repository = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly AddEvent _addEvent;

    public AddEventTests() =>
        _addEvent = new AddEvent(_repository, _time, new CreationQuota(MaxUpcomingEventsPerCreator));

    private static AddEventCommand ValidCommand() => new(
        new GuildId(1), new ChannelId(2), new UserId(3),
        Title: "Réunion", Description: "Salle 2",
        Date: new EventDate.AtTime(Now.AddDays(1)), Duration: TimeSpan.FromMinutes(30));

    private static AddEventCommand AllDayCommand(DateTimeOffset dayStartsAt, DateTimeOffset dayEndsAt) =>
        ValidCommand() with { Date = new EventDate.AllDay(dayStartsAt, dayEndsAt), Duration = null };

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
            () => stored.StartsAt.ShouldBe(Now.AddDays(1)),
            () => stored.Duration.ShouldBe(TimeSpan.FromMinutes(30)),
            () => stored.IsAllDay.ShouldBeFalse());
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
        var command = ValidCommand() with { Date = new EventDate.AtTime(Now.AddMinutes(10)) };
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

    [Fact]
    public async Task Rejects_event_once_the_creator_has_reached_their_quota_of_upcoming_events()
    {
        await AddEventsAsync(MaxUpcomingEventsPerCreator, ValidCommand());

        var result = await _addEvent.HandleAsync(ValidCommand());

        result.Error.ShouldBe(ApplicationErrors.TooManyUpcomingEvents(MaxUpcomingEventsPerCreator));
        _repository.All.Count.ShouldBe(MaxUpcomingEventsPerCreator);
    }

    [Fact]
    public async Task Quota_frees_up_once_the_creator_events_are_past()
    {
        await AddEventsAsync(MaxUpcomingEventsPerCreator, ValidCommand() with { Date = new EventDate.AtTime(Now.AddMinutes(10)) });
        _time.Advance(TimeSpan.FromMinutes(11));

        var result = await _addEvent.HandleAsync(ValidCommand());

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Quota_is_counted_per_creator()
    {
        await AddEventsAsync(MaxUpcomingEventsPerCreator, ValidCommand());

        var result = await _addEvent.HandleAsync(ValidCommand() with { CreatedBy = new UserId(4) });

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Stores_all_day_event_lasting_the_whole_day()
    {
        await _addEvent.HandleAsync(AllDayCommand(TodayStartsAt.AddDays(1), TodayEndsAt.AddDays(1)));

        var stored = _repository.All.ShouldHaveSingleItem();
        stored.ShouldSatisfyAllConditions(
            () => stored.IsAllDay.ShouldBeTrue(),
            () => stored.StartsAt.ShouldBe(TodayStartsAt.AddDays(1)),
            () => stored.Duration.ShouldBe(TimeSpan.FromDays(1)));
    }

    [Fact]
    public async Task Accepts_all_day_event_for_the_current_day()
    {
        var result = await _addEvent.HandleAsync(AllDayCommand(TodayStartsAt, TodayEndsAt));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Rejects_all_day_event_for_a_day_already_over()
    {
        var result = await _addEvent.HandleAsync(AllDayCommand(TodayStartsAt.AddDays(-1), TodayStartsAt));

        result.Error.ShouldBe(DomainErrors.DayInPast);
        _repository.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rejects_all_day_event_with_a_duration()
    {
        var command = AllDayCommand(TodayStartsAt.AddDays(1), TodayEndsAt.AddDays(1)) with { Duration = TimeSpan.FromMinutes(30) };

        var result = await _addEvent.HandleAsync(command);

        result.Error.ShouldBe(ApplicationErrors.DurationWithAllDayEvent);
        _repository.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task All_day_events_of_the_current_day_count_in_the_quota()
    {
        await AddEventsAsync(MaxUpcomingEventsPerCreator, AllDayCommand(TodayStartsAt, TodayEndsAt));

        var result = await _addEvent.HandleAsync(ValidCommand());

        result.Error.ShouldBe(ApplicationErrors.TooManyUpcomingEvents(MaxUpcomingEventsPerCreator));
    }

    private async Task AddEventsAsync(int count, AddEventCommand command)
    {
        for (var i = 0; i < count; i++)
            (await _addEvent.HandleAsync(command)).IsSuccess.ShouldBeTrue();
    }
}
