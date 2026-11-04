using AgendaBot.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AgendaBot.Data;

public class AgendaDbContext(DbContextOptions<AgendaDbContext> options) : DbContext(options)
{
    public DbSet<EventRecord> Events => Set<EventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var readBackAsUtc = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        modelBuilder.Entity<EventRecord>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(EventTitle.MaxLength).IsRequired();
            e.Property(x => x.Description).HasMaxLength(EventDescription.MaxLength);
            e.Property(x => x.StartsAtUtc).HasConversion(readBackAsUtc);
            e.Property(x => x.CreatedAtUtc).HasConversion(readBackAsUtc);
            e.HasIndex(x => new { x.GuildId, x.StartsAtUtc });
        });
    }
}
