namespace AgendaBot.Discord;

internal sealed class UntilFirstSuccess(Func<Task> action)
{
    private bool _succeeded;

    public async Task RunAsync()
    {
        if (_succeeded)
            return;

        await action();
        _succeeded = true;
    }
}
