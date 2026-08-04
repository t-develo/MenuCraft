using FluentAssertions;
using MenuCraft.Api.Data;

namespace MenuCraft.Api.Tests.Data;

public class DatabaseProviderResolverTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_WhenNotConfigured_DefaultsToSqlServer(string? configuredValue)
    {
        var result = DatabaseProviderResolver.Resolve(configuredValue);

        result.Should().Be(DatabaseProvider.SqlServer);
    }

    [Theory]
    [InlineData("Sqlite")]
    [InlineData("sqlite")]
    [InlineData("SQLITE")]
    [InlineData("  Sqlite  ")]
    public void Resolve_WithSqliteValue_ReturnsSqlite(string configuredValue)
    {
        var result = DatabaseProviderResolver.Resolve(configuredValue);

        result.Should().Be(DatabaseProvider.Sqlite);
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("sqlserver")]
    [InlineData("MsSql")]
    public void Resolve_WithSqlServerValue_ReturnsSqlServer(string configuredValue)
    {
        var result = DatabaseProviderResolver.Resolve(configuredValue);

        result.Should().Be(DatabaseProvider.SqlServer);
    }

    [Theory]
    [InlineData("Postgres")]
    [InlineData("sqlite3")]
    [InlineData("MySql")]
    public void Resolve_WithUnknownValue_Throws(string configuredValue)
    {
        var act = () => DatabaseProviderResolver.Resolve(configuredValue);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{configuredValue}*");
    }
}
