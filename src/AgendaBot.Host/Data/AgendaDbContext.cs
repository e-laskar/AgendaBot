using AgendaBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AgendaBot.Data;

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options) { }

    public DbSet<AgendaEvent> Events => Set<AgendaEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite ne conserve pas DateTime.Kind : on force UTC à la relecture (sans effet sous PostgreSQL / timestamptz).
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        modelBuilder.Entity<AgendaEvent>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.StartsAtUtc).HasConversion(utcConverter);
            e.Property(x => x.CreatedAtUtc).HasConversion(utcConverter);
            e.HasIndex(x => new { x.GuildId, x.StartsAtUtc });
        });
    }
}
