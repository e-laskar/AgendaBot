using AgendaBot.Domain;

namespace AgendaBot.UnitTests.Domain;

public class AgendaEventRestoreTests
{
    [Fact]
    public void Restores_persisted_event_without_creation_rules()
    {
        var title = EventTitle.Create("Réunion").Value;
        var description = EventDescription.CreateOptional("Salle 2").Value;
        var pastStart = new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero);

        var evt = AgendaEvent.Restore(
            7, new GuildId(1), new ChannelId(2), new UserId(3),
            title, description, pastStart, TimeSpan.FromMinutes(30), reminderSent: true);

        evt.ShouldSatisfyAllConditions(
            () => evt.Id.ShouldBe(7),
            () => evt.GuildId.ShouldBe(new GuildId(1)),
            () => evt.ChannelId.ShouldBe(new ChannelId(2)),
            () => evt.CreatedBy.ShouldBe(new UserId(3)),
            () => evt.Title.ShouldBe(title),
            () => evt.Description.ShouldBe(description),
            () => evt.StartsAt.ShouldBe(pastStart),
            () => evt.Duration.ShouldBe(TimeSpan.FromMinutes(30)),
            () => evt.ReminderSent.ShouldBeTrue());
    }

    [Fact]
    public void Restored_start_is_in_utc()
    {
        var parisTime = new DateTimeOffset(2026, 12, 24, 20, 0, 0, TimeSpan.FromHours(1));

        var evt = AgendaEvent.Restore(
            7, new GuildId(1), new ChannelId(2), new UserId(3),
            EventTitle.Create("Réunion").Value, null, parisTime, null, reminderSent: false);

        evt.StartsAt.Offset.ShouldBe(TimeSpan.Zero);
    }
}
