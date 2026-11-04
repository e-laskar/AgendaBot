using AgendaBot.Domain;

namespace AgendaBot.UnitTests.Domain;

public class EventTitleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_title(string? value)
    {
        EventTitle.Create(value).Error.ShouldBe(DomainErrors.TitleRequired);
    }

    [Fact]
    public void Accepts_valid_title()
    {
        EventTitle.Create("Réunion").Value.Value.ShouldBe("Réunion");
    }

    [Fact]
    public void Trims_surrounding_spaces()
    {
        EventTitle.Create("  Réunion  ").Value.Value.ShouldBe("Réunion");
    }

    [Fact]
    public void Accepts_title_of_max_length()
    {
        var value = new string('a', EventTitle.MaxLength);

        EventTitle.Create(value).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Rejects_title_longer_than_max_length()
    {
        var value = new string('a', EventTitle.MaxLength + 1);

        EventTitle.Create(value).Error.ShouldBe(DomainErrors.TitleTooLong);
    }

    [Fact]
    public void Checks_length_after_trimming()
    {
        var value = $"  {new string('a', EventTitle.MaxLength)}  ";

        EventTitle.Create(value).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Titles_with_same_text_are_equal()
    {
        EventTitle.Create("Réunion").Value.ShouldBe(EventTitle.Create(" Réunion ").Value);
    }
}
