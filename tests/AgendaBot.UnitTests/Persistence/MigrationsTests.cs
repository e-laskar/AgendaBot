using AgendaBot.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgendaBot.UnitTests.Persistence;

public class MigrationsTests
{
    [Fact]
    public void Migrations_are_up_to_date_with_the_model()
    {
        using var db = new AgendaDbContextDesignTimeFactory().CreateDbContext([]);

        db.Database.HasPendingModelChanges().ShouldBeFalse(
            "le modèle EF a changé : générer une migration avec `dotnet ef migrations add <Nom> --project src/AgendaBot.Persistence`");
    }
}
