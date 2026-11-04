using AgendaBot.Application.UseCases;
using AgendaBot.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgendaBot.UnitTests.Configuration;

public class AgendaUseCasesSetupTests
{
    private const string ConnectionStringNeverOpened = "Host=localhost;Database=agenda_never_opened";

    private static ServiceProvider ServicesConfiguredWith(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

        return new ServiceCollection()
            .AddAgendaPersistence(ConnectionStringNeverOpened)
            .AddAgendaUseCases(configuration)
            .BuildServiceProvider();
    }

    [Fact]
    public void Add_event_use_case_can_be_resolved()
    {
        using var services = ServicesConfiguredWith();

        services.GetService<AddEvent>().ShouldNotBeNull();
    }

    [Fact]
    public void Creation_quota_defaults_to_ten_upcoming_events_per_creator()
    {
        using var services = ServicesConfiguredWith();

        services.GetRequiredService<CreationQuota>().MaxUpcomingEventsPerCreator.ShouldBe(10);
    }

    [Fact]
    public void Creation_quota_can_be_configured()
    {
        using var services = ServicesConfiguredWith(("Agenda:MaxUpcomingEventsPerCreator", "3"));

        services.GetRequiredService<CreationQuota>().MaxUpcomingEventsPerCreator.ShouldBe(3);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Rejects_creation_quota_that_is_not_strictly_positive(string value)
    {
        using var services = ServicesConfiguredWith(("Agenda:MaxUpcomingEventsPerCreator", value));

        Should.Throw<OptionsValidationException>(() => services.GetRequiredService<CreationQuota>())
            .Message.ShouldContain("Agenda__MaxUpcomingEventsPerCreator");
    }
}
