using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Data;

/// <summary>
/// Prepares the database at application startup.
///
/// SQL Server (Azure) deployments keep their existing workflow: the schema is created
/// out-of-band by <c>infra/sql/setup.sql</c>, so this initializer does nothing.
///
/// SQLite (Raspberry Pi) deployments have no equivalent T-SQL script, so the schema is
/// created from the EF Core model instead. This keeps the schema and the model in sync
/// by construction.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        AppDbContext context,
        DatabaseProvider provider,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (provider != DatabaseProvider.Sqlite)
        {
            logger.LogInformation(
                "Database provider is {Provider}; skipping automatic schema creation", provider);
            return;
        }

        EnsureDataDirectoryExists(context.Database.GetConnectionString(), logger);

        var created = await context.Database.EnsureCreatedAsync(ct);
        logger.LogInformation(
            created
                ? "SQLite schema created from the EF Core model"
                : "SQLite schema already present; no changes made");

        // WAL lets readers and the single writer proceed concurrently instead of blocking
        // each other. The setting is stored in the database file, so it survives restarts.
        // The busy timeout is per-connection and comes from the connection string
        // ("Default Timeout", 30 seconds by default in Microsoft.Data.Sqlite).
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
        logger.LogInformation("SQLite journal mode set to WAL");
    }

    /// <summary>
    /// Creates the directory holding the SQLite file when it is missing, so a fresh install
    /// fails with a clear message instead of SQLite's opaque "unable to open database file".
    /// </summary>
    private static void EnsureDataDirectoryExists(string? connectionString, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        string? dataSource;
        try
        {
            dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SQLite の接続文字列を解析できませんでした");
            throw new InvalidOperationException(
                "SQLite の接続文字列が不正です。'Data Source=<パス>' の形式で指定してください。", ex);
        }

        // In-memory databases have no directory to create.
        if (string.IsNullOrWhiteSpace(dataSource) ||
            dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (string.IsNullOrEmpty(directory) || Directory.Exists(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        logger.LogInformation("Created SQLite data directory {Directory}", directory);
    }
}
