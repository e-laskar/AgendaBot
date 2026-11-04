using AgendaBot.Domain;

namespace AgendaBot.UnitTests.Domain;

public class ResultTests
{
    private static readonly Error SomeError = new("some_error", "Une erreur");

    [Fact]
    public void Success_exposes_its_value()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_exposes_its_error()
    {
        Result<int> result = SomeError;

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(SomeError);
    }

    [Fact]
    public void Reading_value_of_a_failure_throws()
    {
        Result<int> result = SomeError;

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Reading_error_of_a_success_throws()
    {
        Result<int> result = 42;

        Should.Throw<InvalidOperationException>(() => result.Error);
    }
}
