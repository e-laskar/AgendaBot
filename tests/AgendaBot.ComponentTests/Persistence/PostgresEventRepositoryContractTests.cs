using AgendaBot.Application.Ports;
using AgendaBot.Persistence;
using AgendaBot.Testing.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace AgendaBot.ComponentTests.Persistence;

public sealed class PostgresEventRepositoryContractTests(PostgresContainer postgres)
    : EventRepositoryContract, IAsyncDisposable
{
    private readonly List<ServiceProvider> _providers = [];

    protected override async Task<IEventRepository> CreateEmptyRepositoryAsync()
    {
        var services = new ServiceCollection()
            .AddAgendaPersistence("Postgres", postgres.ConnectionStringToNewDatabase())
            .BuildServiceProvider();
        _providers.Add(services);

        await services.EnsureAgendaDatabaseCreatedAsync();
        return services.GetRequiredService<IEventRepository>();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var provider in _providers)
            await provider.DisposeAsync();
    }
}
