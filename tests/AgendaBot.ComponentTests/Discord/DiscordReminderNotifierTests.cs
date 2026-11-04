using AgendaBot.Discord.Presentation;
using AgendaBot.Discord.Reminders;
using AgendaBot.Domain;
using AgendaBot.Testing.Builders;

namespace AgendaBot.ComponentTests.Discord;

public class DiscordReminderNotifierTests
{
    private static readonly ChannelId EventChannel = new(111222333444555666);

    private readonly SpyChannelMessenger _messenger = new();
    private readonly DiscordReminderNotifier _notifier;

    public DiscordReminderNotifierTests() => _notifier = new DiscordReminderNotifier(_messenger);

    [Fact]
    public async Task Sends_the_reminder_to_the_channel_where_the_event_was_created()
    {
        var evt = new AnAgendaEvent().InChannel(EventChannel).Titled("Réunion").BuildPersisted(7);

        await _notifier.NotifyAsync(evt, TestContext.Current.CancellationToken);

        var sent = _messenger.Sent.ShouldHaveSingleItem();
        sent.Channel.ShouldBe(EventChannel);
        sent.Message.Text.ShouldBe(AgendaEventMessages.Reminder(evt).Text);
        sent.Message.ShouldPingNobody();
    }

    [Fact]
    public async Task Fails_when_the_channel_cannot_be_reached_so_the_reminder_is_retried()
    {
        var evt = new AnAgendaEvent().InChannel(EventChannel).BuildPersisted(7);
        _messenger.CannotReach(EventChannel);

        var failure = await Should.ThrowAsync<ReminderChannelUnreachableException>(
            () => _notifier.NotifyAsync(evt, TestContext.Current.CancellationToken));

        failure.Channel.ShouldBe(EventChannel);
        _messenger.Sent.ShouldBeEmpty();
    }

    private sealed class SpyChannelMessenger : IChannelMessenger
    {
        private readonly HashSet<ChannelId> _unreachable = [];
        private readonly List<(ChannelId Channel, OutgoingMessage Message)> _sent = [];

        public IReadOnlyList<(ChannelId Channel, OutgoingMessage Message)> Sent => _sent;

        public void CannotReach(ChannelId channel) => _unreachable.Add(channel);

        public Task<bool> TrySendAsync(ChannelId channel, OutgoingMessage message, CancellationToken cancellationToken)
        {
            if (_unreachable.Contains(channel))
                return Task.FromResult(false);

            _sent.Add((channel, message));
            return Task.FromResult(true);
        }
    }
}
