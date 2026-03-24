using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenDeepWiki.Postgresql;

/// <summary>
/// Design-time database context factory for EF Core migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgresqlDbContext>
{
    public PostgresqlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgresqlDbContext>();
        // Use a dummy connection string for migration generation only
        optionsBuilder.UseNpgsql("Host=localhost;Database=opendeepwiki;Username=postgres;Password=postgres");
        return new PostgresqlDbContext(optionsBuilder.Options);
    }
}
