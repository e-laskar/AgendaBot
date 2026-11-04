using Npgsql;
using Testcontainers.PostgreSql;

namespace AgendaBot.ComponentTests.Persistence;

[CollectionDefinition(Name)]
public sealed class RequiresPostgres : ICollectionFixture<PostgresContainer>
{
    public const string Name = "Requires PostgreSQL (Docker)";
}

public sealed class PostgresContainer : IAsyncLifetime
{
    private const string SameImageAsDockerCompose = "postgres:17-alpine";

    private PostgreSqlContainer? _container;

    private PostgreSqlContainer Container =>
        _container ?? throw new InvalidOperationException("Le conteneur PostgreSQL n'est pas démarré.");

    public async ValueTask InitializeAsync()
    {
        _container = new PostgreSqlBuilder(SameImageAsDockerCompose).Build();
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public string ConnectionStringToNewDatabase() =>
        new NpgsqlConnectionStringBuilder(Container.GetConnectionString())
        {
            Database = $"agenda_{Guid.NewGuid():N}"
        }.ConnectionString;
}
