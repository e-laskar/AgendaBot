using AgendaBot.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgendaBot.Persistence;

public class AgendaDbContext(DbContextOptions<AgendaDbContext> options) : DbContext(options)
{
    public DbSet<EventRecord> Events => Set<EventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventRecord>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(EventTitle.MaxLength).IsRequired();
            e.Property(x => x.Description).HasMaxLength(EventDescription.MaxLength);
            e.HasIndex(x => new { x.GuildId, x.StartsAtUtc });
        });
    }
}
