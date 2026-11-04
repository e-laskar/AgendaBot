using AgendaBot.Discord;

namespace AgendaBot.ComponentTests.Discord;

public class UntilFirstSuccessTests
{
    private int _runs;
    private bool _failing;

    private Task Action()
    {
        _runs++;
        return _failing ? throw new InvalidOperationException("Enregistrement refusé") : Task.CompletedTask;
    }

    [Fact]
    public async Task Runs_the_action_the_first_time()
    {
        var registration = new UntilFirstSuccess(Action);

        await registration.RunAsync();

        _runs.ShouldBe(1);
    }

    [Fact]
    public async Task Does_not_run_the_action_again_once_it_succeeded()
    {
        var registration = new UntilFirstSuccess(Action);

        await registration.RunAsync();
        await registration.RunAsync();
        await registration.RunAsync();

        _runs.ShouldBe(1);
    }

    [Fact]
    public async Task Lets_the_failure_through_and_runs_again_next_time()
    {
        var registration = new UntilFirstSuccess(Action);
        _failing = true;
        await Should.ThrowAsync<InvalidOperationException>(registration.RunAsync);

        _failing = false;
        await registration.RunAsync();
        await registration.RunAsync();

        _runs.ShouldBe(2);
    }
}
