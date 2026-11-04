using AgendaBot.Domain;

namespace AgendaBot.UnitTests.Domain;

public class EventDescriptionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_description_means_no_description(string? value)
    {
        var result = EventDescription.CreateOptional(value);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void Accepts_valid_description()
    {
        EventDescription.CreateOptional("Salle 2").Value!.Value.ShouldBe("Salle 2");
    }

    [Fact]
    public void Trims_surrounding_spaces()
    {
        EventDescription.CreateOptional("  Salle 2  ").Value!.Value.ShouldBe("Salle 2");
    }

    [Fact]
    public void Keeps_inner_line_breaks()
    {
        EventDescription.CreateOptional("Ordre du jour :\n- budget").Value!.Value
            .ShouldBe("Ordre du jour :\n- budget");
    }

    [Fact]
    public void Accepts_description_of_max_length()
    {
        var value = new string('a', EventDescription.MaxLength);

        EventDescription.CreateOptional(value).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Rejects_description_longer_than_max_length()
    {
        var value = new string('a', EventDescription.MaxLength + 1);

        EventDescription.CreateOptional(value).Error.ShouldBe(DomainErrors.DescriptionTooLong);
    }
}
