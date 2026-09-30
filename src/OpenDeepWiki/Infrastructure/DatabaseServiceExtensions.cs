using OpenDeepWiki.EFCore;

namespace OpenDeepWiki.Infrastructure;

/// <summary>
/// Database service extensions
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Add database services according to configuration
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dbType = configuration.GetValue<string>("Database:Type")?.ToLowerInvariant()
            ?? Environment.GetEnvironmentVariable("DB_TYPE")?.ToLowerInvariant()
            ?? "sqlite";

        var connectionString = configuration.GetConnectionString("Default")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? GetDefaultConnectionString(dbType);

        return dbType switch
        {
            "sqlite" => AddSqlite(services, connectionString),
            "postgresql" or "postgres" => AddPostgresql(services, connectionString),
            _ => throw new InvalidOperationException($"Unsupported database type: {dbType}. Supported types: sqlite, postgresql")
        };
    }

    private static IServiceCollection AddSqlite(IServiceCollection services, string connectionString)
    {
        // Dynamically load the Sqlite assembly
        var assembly = LoadProviderAssembly("OpenDeepWiki.Sqlite");
        var extensionType = assembly.GetType("OpenDeepWiki.Sqlite.SqliteServiceCollectionExtensions")
            ?? throw new InvalidOperationException("SqliteServiceCollectionExtensions type not found");

        var method = extensionType.GetMethod("AddOpenDeepWikiSqlite")
            ?? throw new InvalidOperationException("AddOpenDeepWikiSqlite method not found");

        method.Invoke(null, [services, connectionString]);
        return services;
    }

    private static IServiceCollection AddPostgresql(IServiceCollection services, string connectionString)
    {
        // Dynamically load the Postgresql assembly
        var assembly = LoadProviderAssembly("OpenDeepWiki.Postgresql");
        var extensionType = assembly.GetType("OpenDeepWiki.Postgresql.PostgresqlServiceCollectionExtensions")
            ?? throw new InvalidOperationException("PostgresqlServiceCollectionExtensions type not found");

        var method = extensionType.GetMethod("AddOpenDeepWikiPostgresql")
            ?? throw new InvalidOperationException("AddOpenDeepWikiPostgresql method not found");

        method.Invoke(null, [services, connectionString]);
        return services;
    }

    private static System.Reflection.Assembly LoadProviderAssembly(string assemblyName)
    {
        try
        {
            return System.Reflection.Assembly.Load(assemblyName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Unable to load database provider assembly '{assemblyName}'. Make sure the corresponding project reference has been added.", ex);
        }
    }

    private static string GetDefaultConnectionString(string dbType)
    {
        return dbType switch
        {
            "sqlite" => "Data Source=opendeepwiki.db",
            "postgresql" or "postgres" => "Host=localhost;Database=opendeepwiki;Username=postgres;Password=postgres",
            _ => throw new InvalidOperationException($"Unknown database type: {dbType}")
        };
    }
}
