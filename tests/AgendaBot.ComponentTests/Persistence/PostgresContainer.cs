using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(AgendaBot.ComponentTests.Persistence.PostgresContainer))]

namespace AgendaBot.ComponentTests.Persistence;

public sealed class PostgresContainer : IAsyncLifetime
{
    private const string SameImageAsDockerCompose = "postgres:17-alpine";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(SameImageAsDockerCompose).Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    public string ConnectionStringToNewDatabase() =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = $"agenda_{Guid.NewGuid():N}"
        }.ConnectionString;
}
