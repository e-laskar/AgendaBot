using AgendaBot.Application.Ports;
using AgendaBot.Domain;
using AgendaBot.Testing.Builders;

namespace AgendaBot.Testing.Contracts;

public abstract class EventRepositoryContract
{
    private static readonly DateTimeOffset T0 = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly GuildId Guild = new(1234567890123456789);
    private static readonly GuildId OtherGuild = new(987654321098765432);

    protected abstract Task<IEventRepository> CreateEmptyRepositoryAsync();

    private static AnAgendaEvent AnEvent() =>
        new AnAgendaEvent().InGuild(Guild).CreatedAt(T0.AddYears(-10)).StartingAt(T0);

    [Fact]
    public async Task Added_events_get_distinct_ids()
    {
        var repository = await CreateEmptyRepositoryAsync();

        var first = await repository.AddAsync(AnEvent().Build());
        var second = await repository.AddAsync(AnEvent().Build());

        first.Id.ShouldNotBe(0);
        second.Id.ShouldNotBe(first.Id);
    }

    [Fact]
    public async Task Added_event_is_read_back_identically()
    {
        var repository = await CreateEmptyRepositoryAsync();
        var original = AnEvent()
            .InChannel(new ChannelId(111222333444555666))
            .CreatedBy(new UserId(777888999000111222))
            .Titled("Réunion d'équipe")
            .DescribedAs("Ordre du jour :\n- budget")
            .StartingAt(T0.AddMinutes(90))
            .Lasting(TimeSpan.FromMinutes(45))
            .Build();

        var added = await repository.AddAsync(original);
        var readBack = await repository.GetAsync(Guild, added.Id);

        ShouldBeSameEvent(readBack, original, added.Id);
    }

    [Fact]
    public async Task Event_without_description_nor_duration_is_read_back_identically()
    {
        var repository = await CreateEmptyRepositoryAsync();
        var original = AnEvent().DescribedAs(null).Lasting(null).Build();

        var added = await repository.AddAsync(original);
        var readBack = await repository.GetAsync(Guild, added.Id);

        ShouldBeSameEvent(readBack, original, added.Id);
    }

    [Fact]
    public async Task Unknown_event_is_not_found()
    {
        var repository = await CreateEmptyRepositoryAsync();

        (await repository.GetAsync(Guild, 42)).ShouldBeNull();
    }

    [Fact]
    public async Task Event_of_another_guild_is_not_found()
    {
        var repository = await CreateEmptyRepositoryAsync();
        var added = await repository.AddAsync(AnEvent().InGuild(OtherGuild).Build());

        (await repository.GetAsync(Guild, added.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Upcoming_events_are_sorted_by_start_date()
    {
        var repository = await CreateEmptyRepositoryAsync();
        await repository.AddAsync(AnEvent().Titled("Troisième").StartingAt(T0.AddDays(3)).Build());
        await repository.AddAsync(AnEvent().Titled("Premier").StartingAt(T0.AddDays(1)).Build());
        await repository.AddAsync(AnEvent().Titled("Deuxième").StartingAt(T0.AddDays(2)).Build());

        var upcoming = await repository.GetUpcomingByStartDateAsync(Guild, T0, max: 10);

        upcoming.Select(e => e.Title.Value).ShouldBe(["Premier", "Deuxième", "Troisième"]);
    }

    [Fact]
    public async Task Upcoming_events_start_from_the_given_date_included()
    {
        var repository = await CreateEmptyRepositoryAsync();
        await repository.AddAsync(AnEvent().Titled("Juste avant").StartingAt(T0.AddMinutes(-1)).Build());
        await repository.AddAsync(AnEvent().Titled("Pile à l'heure").StartingAt(T0).Build());

        var upcoming = await repository.GetUpcomingByStartDateAsync(Guild, T0, max: 10);

        upcoming.ShouldHaveSingleItem().Title.Value.ShouldBe("Pile à l'heure");
    }

    [Fact]
    public async Task Upcoming_events_exclude_other_guilds()
    {
        var repository = await CreateEmptyRepositoryAsync();
        await repository.AddAsync(AnEvent().InGuild(OtherGuild).Build());
        var ours = await repository.AddAsync(AnEvent().InGuild(Guild).Build());

        var upcoming = await repository.GetUpcomingByStartDateAsync(Guild, T0, max: 10);

        upcoming.ShouldHaveSingleItem().Id.ShouldBe(ours.Id);
    }

    [Fact]
    public async Task Upcoming_events_are_limited_to_the_requested_number()
    {
        var repository = await CreateEmptyRepositoryAsync();
        for (var day = 1; day <= 5; day++)
            await repository.AddAsync(AnEvent().Titled($"Jour {day}").StartingAt(T0.AddDays(day)).Build());

        var upcoming = await repository.GetUpcomingByStartDateAsync(Guild, T0, max: 3);

        upcoming.Select(e => e.Title.Value).ShouldBe(["Jour 1", "Jour 2", "Jour 3"]);
    }

    [Fact]
    public async Task Reminder_candidates_start_strictly_after_lower_bound_and_up_to_upper_bound()
    {
        var repository = await CreateEmptyRepositoryAsync();
        foreach (var minutes in new[] { 0, 1, 15, 16 })
            await repository.AddAsync(AnEvent().Titled($"+{minutes}").StartingAt(T0.AddMinutes(minutes)).Build());

        var candidates = await repository.GetWithoutReminderStartingBetweenAsync(T0, T0.AddMinutes(15));

        candidates.Select(e => e.Title.Value).OrderBy(t => t).ShouldBe(["+1", "+15"]);
    }

    [Fact]
    public async Task Reminder_candidates_exclude_events_already_reminded()
    {
        var repository = await CreateEmptyRepositoryAsync();
        var reminded = await repository.AddAsync(AnEvent().StartingAt(T0.AddMinutes(5)).Build());
        var notReminded = await repository.AddAsync(AnEvent().StartingAt(T0.AddMinutes(5)).Build());
        await repository.TryClaimReminderAsync(reminded);

        var candidates = await repository.GetWithoutReminderStartingBetweenAsync(T0, T0.AddMinutes(15));

        candidates.ShouldHaveSingleItem().Id.ShouldBe(notReminded.Id);
    }

    [Fact]
    public async Task Reminder_candidates_include_every_guild()
    {
        var repository = await CreateEmptyRepositoryAsync();
        await repository.AddAsync(AnEvent().InGuild(Guild).StartingAt(T0.AddMinutes(5)).Build());
        await repository.AddAsync(AnEvent().InGuild(OtherGuild).StartingAt(T0.AddMinutes(5)).Build());

        var candidates = await repository.GetWithoutReminderStartingBetweenAsync(T0, T0.AddMinutes(15));

        candidates.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Claimed_reminder_is_read_back_as_sent()
    {
        var repository = await CreateEmptyRepositoryAsync();
        var added = await repository.AddAsync(AnEvent().Build());

        (await repository.TryClaimReminderAsync(added)).ShouldBeTrue();

        (await repository.GetAsync(Guild, added.Id))!.ReminderSent.ShouldBeTrue();
    }

    [Fact]
    public async Task Reminder_can_be_claimed_only_once()
    {
        var repository = await CreateEmptyRepositoryAsync();
        var added = await repository.AddAsync(AnEvent().Build());

        var firstClaim = await repository.TryClaimReminderAsync(added);
        var secondClaim = await repository.TryClaimReminderAsync(added);

        firstClaim.ShouldBeTrue();
        secondClaim.ShouldBeFalse();
    }

    [Fact]
    public async Task Released_reminder_becomes_a_candidate_again()
    {
        var repository = await CreateEmptyRepositoryAsync();
        var added = await repository.AddAsync(AnEvent().StartingAt(T0.AddMinutes(5)).Build());
        await repository.TryClaimReminderAsync(added);

        await repository.ReleaseReminderClaimAsync(added);

        (await repository.GetWithoutReminderStartingBetweenAsync(T0, T0.AddMinutes(15)))
            .ShouldHaveSingleItem().Id.ShouldBe(added.Id);
    }

    [Fact]
    public async Task Released_reminder_can_be_claimed_again()
    {
        var repository = await CreateEmptyRepositoryAsync();
        var added = await repository.AddAsync(AnEvent().Build());
        await repository.TryClaimReminderAsync(added);
        await repository.ReleaseReminderClaimAsync(added);

        (await repository.TryClaimReminderAsync(added)).ShouldBeTrue();
    }

    [Fact]
    public async Task Deleted_event_is_no_longer_found_and_the_others_remain()
    {
        var repository = await CreateEmptyRepositoryAsync();
        var deleted = await repository.AddAsync(AnEvent().Build());
        var kept = await repository.AddAsync(AnEvent().Build());

        await repository.DeleteAsync(deleted);

        (await repository.GetAsync(Guild, deleted.Id)).ShouldBeNull();
        (await repository.GetAsync(Guild, kept.Id)).ShouldNotBeNull();
    }

    private static void ShouldBeSameEvent(AgendaEvent? actual, AgendaEvent expected, int expectedId)
    {
        actual.ShouldNotBeNull();
        actual.ShouldSatisfyAllConditions(
            () => actual.Id.ShouldBe(expectedId),
            () => actual.GuildId.ShouldBe(expected.GuildId),
            () => actual.ChannelId.ShouldBe(expected.ChannelId),
            () => actual.CreatedBy.ShouldBe(expected.CreatedBy),
            () => actual.Title.ShouldBe(expected.Title),
            () => actual.Description.ShouldBe(expected.Description),
            () => actual.StartsAt.ShouldBe(expected.StartsAt),
            () => actual.StartsAt.Offset.ShouldBe(TimeSpan.Zero),
            () => actual.Duration.ShouldBe(expected.Duration),
            () => actual.ReminderSent.ShouldBe(expected.ReminderSent));
    }
}
