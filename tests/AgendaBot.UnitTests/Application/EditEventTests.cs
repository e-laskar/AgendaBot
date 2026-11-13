using AgendaBot.Application;
using AgendaBot.Application.UseCases;
using AgendaBot.Domain;
using AgendaBot.Testing.Builders;
using AgendaBot.Testing.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace AgendaBot.UnitTests.Application;

public class EditEventTests
{
    private static readonly DateTimeOffset Now = AnAgendaEvent.DefaultNow;
    private static readonly DateTimeOffset Tomorrow = Now.AddDays(1);
    private static readonly DateTimeOffset TomorrowStartsAt = new(2026, 11, 4, 23, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TomorrowEndsAt = TomorrowStartsAt.AddDays(1);
    private static readonly GuildId Guild = AnAgendaEvent.DefaultGuild;
    private static readonly UserId Creator = new(30);
    private static readonly UserId AnotherMember = new(31);
    private const int EventId = 7;

    private readonly InMemoryEventRepository _repository = new();
    private readonly EditEvent _editEvent;

    public EditEventTests() => _editEvent = new EditEvent(_repository, new FakeTimeProvider(Now));

    private static AnAgendaEvent AnEvent() =>
        new AnAgendaEvent().InGuild(Guild).CreatedBy(Creator)
            .Titled("Réunion").DescribedAs("Salle 2")
            .StartingAt(Tomorrow).Lasting(TimeSpan.FromMinutes(30));

    private static EditEventCommand ByCreator() => new(
        Guild, EventId, Creator, CanManageEvents: false,
        Title: null, Description: null, Date: null, Duration: null);

    private AgendaEvent Stored() => _repository.All.ShouldHaveSingleItem();

    [Fact]
    public async Task Changes_only_the_title_when_only_the_title_is_given()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        await _editEvent.HandleAsync(ByCreator() with { Title = "Nouveau titre" });

        var stored = Stored();
        stored.ShouldSatisfyAllConditions(
            () => stored.Title.Value.ShouldBe("Nouveau titre"),
            () => stored.Description!.Value.ShouldBe("Salle 2"),
            () => stored.StartsAt.ShouldBe(Tomorrow),
            () => stored.Duration.ShouldBe(TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public async Task Changes_only_the_description_when_only_the_description_is_given()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        await _editEvent.HandleAsync(ByCreator() with { Description = "Salle 3" });

        Stored().Title.Value.ShouldBe("Réunion");
        Stored().Description!.Value.ShouldBe("Salle 3");
    }

    [Fact]
    public async Task Returns_the_edited_event()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(ByCreator() with { Title = "Nouveau titre" });

        result.Value.Id.ShouldBe(EventId);
        result.Value.Title.Value.ShouldBe("Nouveau titre");
    }

    [Fact]
    public async Task Moving_to_another_time_keeps_the_duration()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        await _editEvent.HandleAsync(ByCreator() with { Date = new EventDate.AtTime(Tomorrow.AddHours(2)) });

        Stored().StartsAt.ShouldBe(Tomorrow.AddHours(2));
        Stored().Duration.ShouldBe(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task Changes_only_the_duration_when_only_the_duration_is_given()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        await _editEvent.HandleAsync(ByCreator() with { Duration = TimeSpan.FromHours(2) });

        Stored().StartsAt.ShouldBe(Tomorrow);
        Stored().Duration.ShouldBe(TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task Moves_a_timed_event_to_a_whole_day()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        await _editEvent.HandleAsync(ByCreator() with { Date = new EventDate.AllDay(TomorrowStartsAt, TomorrowEndsAt) });

        Stored().IsAllDay.ShouldBeTrue();
        Stored().StartsAt.ShouldBe(TomorrowStartsAt);
    }

    [Fact]
    public async Task Moving_an_all_day_event_to_a_time_does_not_carry_the_length_of_the_day()
    {
        _repository.Seed(AnEvent().OnWholeDay(TomorrowStartsAt, TomorrowEndsAt).BuildPersisted(EventId));

        await _editEvent.HandleAsync(ByCreator() with { Date = new EventDate.AtTime(Tomorrow) });

        Stored().IsAllDay.ShouldBeFalse();
        Stored().Duration.ShouldBeNull();
    }

    [Fact]
    public async Task Rejects_a_duration_on_an_event_moved_to_a_whole_day()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(ByCreator() with
        {
            Date = new EventDate.AllDay(TomorrowStartsAt, TomorrowEndsAt),
            Duration = TimeSpan.FromHours(1)
        });

        result.Error.ShouldBe(ApplicationErrors.DurationWithAllDayEvent);
        Stored().IsAllDay.ShouldBeFalse();
    }

    [Fact]
    public async Task Rejects_a_duration_alone_on_an_all_day_event()
    {
        _repository.Seed(AnEvent().OnWholeDay(TomorrowStartsAt, TomorrowEndsAt).BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(ByCreator() with { Duration = TimeSpan.FromHours(1) });

        result.Error.ShouldBe(ApplicationErrors.DurationWithAllDayEvent);
    }

    [Fact]
    public async Task Moving_to_another_date_makes_the_reminder_due_again()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId, reminderSent: true));

        await _editEvent.HandleAsync(ByCreator() with { Date = new EventDate.AtTime(Tomorrow.AddHours(2)) });

        Stored().ReminderSent.ShouldBeFalse();
    }

    [Fact]
    public async Task Changing_the_title_keeps_the_reminder_already_sent()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId, reminderSent: true));

        await _editEvent.HandleAsync(ByCreator() with { Title = "Nouveau titre" });

        Stored().ReminderSent.ShouldBeTrue();
    }

    [Fact]
    public async Task Member_with_manage_events_permission_can_edit_an_event_of_someone_else()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(
            ByCreator() with { EditedBy = AnotherMember, CanManageEvents = true, Title = "Nouveau titre" });

        result.IsSuccess.ShouldBeTrue();
        Stored().Title.Value.ShouldBe("Nouveau titre");
    }

    [Fact]
    public async Task Rejects_a_member_who_neither_created_the_event_nor_manages_events()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(ByCreator() with { EditedBy = AnotherMember, Title = "Nouveau titre" });

        result.Error.ShouldBe(ApplicationErrors.NotAllowedToEditEvent);
        Stored().Title.Value.ShouldBe("Réunion");
    }

    [Fact]
    public async Task Rejects_an_edit_without_any_change()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(ByCreator());

        result.Error.ShouldBe(ApplicationErrors.NothingToEdit);
    }

    [Fact]
    public async Task Rejects_an_event_already_past()
    {
        _repository.Seed(AnEvent().StartingAt(Now.AddMinutes(-1)).BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(ByCreator() with { Date = new EventDate.AtTime(Tomorrow) });

        result.Error.ShouldBe(DomainErrors.EventAlreadyPast);
        Stored().StartsAt.ShouldBe(Now.AddMinutes(-1));
    }

    [Fact]
    public async Task Rejects_a_new_date_before_current_time()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(ByCreator() with { Date = new EventDate.AtTime(Now.AddMinutes(-1)) });

        result.Error.ShouldBe(DomainErrors.StartInPast);
        Stored().StartsAt.ShouldBe(Tomorrow);
    }

    [Fact]
    public async Task Rejects_an_invalid_title_without_changing_anything()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(ByCreator() with
        {
            Title = "  ",
            Date = new EventDate.AtTime(Tomorrow.AddHours(2))
        });

        result.Error.ShouldBe(DomainErrors.TitleRequired);
        Stored().StartsAt.ShouldBe(Tomorrow);
    }

    [Fact]
    public async Task Rejects_an_invalid_description()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(
            ByCreator() with { Description = new string('a', EventDescription.MaxLength + 1) });

        result.Error.ShouldBe(DomainErrors.DescriptionTooLong);
    }

    [Fact]
    public async Task Fails_when_the_event_does_not_exist()
    {
        var result = await _editEvent.HandleAsync(ByCreator() with { Title = "Nouveau titre" });

        result.Error.ShouldBe(ApplicationErrors.EventNotFound(EventId));
    }

    [Fact]
    public async Task Cannot_edit_an_event_of_another_guild()
    {
        _repository.Seed(AnEvent().InGuild(new GuildId(99)).BuildPersisted(EventId));

        var result = await _editEvent.HandleAsync(ByCreator() with { Title = "Nouveau titre" });

        result.Error.ShouldBe(ApplicationErrors.EventNotFound(EventId));
        Stored().Title.Value.ShouldBe("Réunion");
    }

    [Fact]
    public async Task Fails_when_the_event_is_deleted_while_being_edited()
    {
        _repository.Seed(AnEvent().BuildPersisted(EventId));
        _repository.AnotherInstanceDeletesBeforeUpdate(EventId);

        var result = await _editEvent.HandleAsync(ByCreator() with { Title = "Nouveau titre" });

        result.Error.ShouldBe(ApplicationErrors.EventNotFound(EventId));
        _repository.All.ShouldBeEmpty();
    }
}
