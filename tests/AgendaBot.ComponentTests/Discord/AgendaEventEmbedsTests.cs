using AgendaBot.Discord.Presentation;
using AgendaBot.Domain;
using AgendaBot.Testing.Builders;

namespace AgendaBot.ComponentTests.Discord;

public class AgendaEventEmbedsTests
{
    private static string FieldValue(global::Discord.Embed embed, string name) =>
        embed.Fields.Single(f => f.Name == name).Value;

    [Fact]
    public void Details_show_title_description_creator_and_id()
    {
        var evt = new AnAgendaEvent().Titled("Réunion").DescribedAs("Salle 2").CreatedBy(new UserId(42)).BuildPersisted(7);

        var embed = AgendaEventEmbeds.Details(evt);

        embed.ShouldSatisfyAllConditions(
            () => embed.Title.ShouldBe("📌 Réunion"),
            () => embed.Description.ShouldBe("Salle 2"),
            () => FieldValue(embed, "Créé par").ShouldBe("<@42>"),
            () => embed.Footer!.Value.Text.ShouldBe("ID : 7"));
    }

    [Fact]
    public void Details_show_start_as_full_date_and_relative_time_in_viewer_time_zone()
    {
        var evt = new AnAgendaEvent().BuildPersisted(7);
        var unix = evt.StartsAt.ToUnixTimeSeconds();

        var embed = AgendaEventEmbeds.Details(evt);

        FieldValue(embed, "Début").ShouldBe($"<t:{unix}:F>\n<t:{unix}:R>");
    }

    [Fact]
    public void Details_say_when_there_is_no_description()
    {
        var evt = new AnAgendaEvent().DescribedAs(null).BuildPersisted(7);

        AgendaEventEmbeds.Details(evt).Description.ShouldBe("_Pas de description_");
    }

    [Theory]
    [InlineData(45, "0h45")]
    [InlineData(90, "1h30")]
    [InlineData(1500, "25h00")]
    public void Details_show_duration_in_hours_and_minutes(int minutes, string expected)
    {
        var evt = new AnAgendaEvent().Lasting(TimeSpan.FromMinutes(minutes)).BuildPersisted(7);

        FieldValue(AgendaEventEmbeds.Details(evt), "Durée").ShouldBe(expected);
    }

    [Fact]
    public void Details_have_no_duration_field_when_duration_is_unknown()
    {
        var evt = new AnAgendaEvent().Lasting(null).BuildPersisted(7);

        AgendaEventEmbeds.Details(evt).Fields.ShouldNotContain(f => f.Name == "Durée");
    }

    [Fact]
    public void Upcoming_list_shows_one_field_per_event_in_the_given_order()
    {
        var first = new AnAgendaEvent().Titled("Réunion").BuildPersisted(1);
        var second = new AnAgendaEvent().Titled("Apéro").StartingAt(first.StartsAt.AddDays(1)).BuildPersisted(2);
        var unix = first.StartsAt.ToUnixTimeSeconds();

        var embed = AgendaEventEmbeds.UpcomingList([first, second]);

        embed.Fields.Select(f => f.Name).ShouldBe(["#1 · Réunion", "#2 · Apéro"]);
        embed.Fields[0].Value.ShouldBe($"<t:{unix}:F> (<t:{unix}:R>)");
    }
}
