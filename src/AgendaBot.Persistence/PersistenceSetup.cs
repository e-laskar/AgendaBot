using AgendaBot.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AgendaBot.Persistence;

public static class PersistenceSetup
{
    public static IServiceCollection AddAgendaPersistence(
        this IServiceCollection services, string provider, string connectionString)
    {
        services.AddDbContextFactory<AgendaDbContext>(options => UseProvider(options, provider, connectionString));
        services.AddSingleton<IEventRepository, EfEventRepository>();
        return services;
    }

    public static async Task EnsureAgendaDatabaseCreatedAsync(this IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<AgendaDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
    }

    private static void UseProvider(DbContextOptionsBuilder options, string provider, string connectionString)
    {
        switch (provider.ToLowerInvariant())
        {
            case "sqlite":
                options.UseSqlite(connectionString);
                break;
            case "postgres" or "postgresql" or "npgsql":
                options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
                break;
            default:
                throw new InvalidOperationException(
                    $"Database__Provider inconnu : « {provider} » (valeurs possibles : Sqlite, Postgres).");
        }
    }
}
