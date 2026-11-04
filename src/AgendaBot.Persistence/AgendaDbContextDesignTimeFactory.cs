using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgendaBot.Persistence;

public sealed class AgendaDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AgendaDbContext>
{
    private const string ConnectionStringNeverOpenedWhenGeneratingMigrations =
        "Host=localhost;Database=agenda_design_time";

    public AgendaDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AgendaDbContext>()
            .UseNpgsql(ConnectionStringNeverOpenedWhenGeneratingMigrations)
            .Options);
}
