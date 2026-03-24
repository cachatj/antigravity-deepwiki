using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenDeepWiki.Sqlite;

/// <summary>
/// Design-time database context factory for EF Core migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
    public SqliteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>();
        // Use a dummy connection string for migration generation only
        optionsBuilder.UseSqlite("Data Source=opendeepwiki.db");
        return new SqliteDbContext(optionsBuilder.Options);
    }
}
