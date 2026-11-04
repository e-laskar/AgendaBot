using AgendaBot.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AgendaBot.Persistence;

public static class PersistenceSetup
{
    public static IServiceCollection AddAgendaPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<AgendaDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));
        services.AddSingleton<IEventRepository, EfEventRepository>();
        return services;
    }

    public static async Task MigrateAgendaDatabaseAsync(this IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<AgendaDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }
}
