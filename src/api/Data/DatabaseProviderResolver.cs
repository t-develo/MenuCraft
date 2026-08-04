namespace MenuCraft.Api.Data;

/// <summary>
/// Supported EF Core database providers.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>Azure SQL Database / SQL Server (cloud deployment).</summary>
    SqlServer,

    /// <summary>SQLite (Raspberry Pi / local self-hosted deployment).</summary>
    Sqlite,
}

/// <summary>
/// Resolves the configured database provider from the <c>Database:Provider</c> setting.
/// Defaults to <see cref="DatabaseProvider.SqlServer"/> so existing Azure deployments,
/// which do not set the value, keep their previous behaviour.
/// </summary>
public static class DatabaseProviderResolver
{
    public const string ConfigurationKey = "Database:Provider";

    /// <summary>
    /// Maps a configuration value to a <see cref="DatabaseProvider"/>.
    /// </summary>
    /// <param name="configuredValue">Raw configuration value; may be null or whitespace.</param>
    /// <exception cref="InvalidOperationException">The value is not a recognised provider name.</exception>
    public static DatabaseProvider Resolve(string? configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return DatabaseProvider.SqlServer;
        }

        return configuredValue.Trim() switch
        {
            var v when v.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) => DatabaseProvider.SqlServer,
            var v when v.Equals("MsSql", StringComparison.OrdinalIgnoreCase) => DatabaseProvider.SqlServer,
            var v when v.Equals("Sqlite", StringComparison.OrdinalIgnoreCase) => DatabaseProvider.Sqlite,
            _ => throw new InvalidOperationException(
                $"'{ConfigurationKey}' の値 '{configuredValue}' は不正です。'SqlServer' または 'Sqlite' を指定してください。"),
        };
    }
}
