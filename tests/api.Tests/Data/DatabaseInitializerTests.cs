using FluentAssertions;
using MenuCraft.Api.Data;
using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuCraft.Api.Tests.Data;

/// <summary>
/// Integration tests against a real SQLite file, verifying that the Raspberry Pi
/// deployment path can create and use its schema.
/// </summary>
public class DatabaseInitializerTests : IDisposable
{
    private readonly string _directory;
    private readonly string _databasePath;
    private readonly ILogger _logger = Mock.Of<ILogger>();

    public DatabaseInitializerTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"menucraft-tests-{Guid.NewGuid():N}");
        _databasePath = Path.Combine(_directory, "data", "menucraft.db");
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task InitializeAsync_WithSqlite_CreatesTheDataDirectoryAndSchema()
    {
        await using var context = CreateContext();

        await DatabaseInitializer.InitializeAsync(context, DatabaseProvider.Sqlite, _logger);

        File.Exists(_databasePath).Should().BeTrue();

        // Spot-check tables from both the Identity and the application model.
        var tables = await GetTableNamesAsync(context);
        tables.Should().Contain(new[]
        {
            "AspNetUsers", "AspNetRoles", "AspNetUserRoles",
            "FamilyGroups", "Recipes", "RecipeTags", "RecipeIngredients",
            "MealPlans", "MealPlanRecipes", "ShoppingListChecks", "RefreshTokens",
        });
    }

    [Fact]
    public async Task InitializeAsync_WithSqlite_EnablesWalJournalMode()
    {
        await using var context = CreateContext();

        await DatabaseInitializer.InitializeAsync(context, DatabaseProvider.Sqlite, _logger);

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        var journalMode = (string?)await command.ExecuteScalarAsync();

        journalMode.Should().BeEquivalentTo("wal");
    }

    [Fact]
    public async Task InitializeAsync_WithSqlite_IsIdempotent()
    {
        await using (var first = CreateContext())
        {
            await DatabaseInitializer.InitializeAsync(first, DatabaseProvider.Sqlite, _logger);
        }

        await using var second = CreateContext();
        var act = () => DatabaseInitializer.InitializeAsync(second, DatabaseProvider.Sqlite, _logger);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_WithSqlServer_DoesNotTouchTheDatabase()
    {
        await using var context = CreateContext();

        await DatabaseInitializer.InitializeAsync(context, DatabaseProvider.SqlServer, _logger);

        // The SQL Server path relies on infra/sql/setup.sql, so nothing should be created here.
        File.Exists(_databasePath).Should().BeFalse();
    }

    [Fact]
    public async Task SqliteSchema_RoundTripsDateOnlyAndEnumValues()
    {
        await using var context = CreateContext();
        await DatabaseInitializer.InitializeAsync(context, DatabaseProvider.Sqlite, _logger);

        var group = new FamilyGroup { Name = "テスト家族", InviteCode = "ABC12345" };
        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        var date = new DateOnly(2026, 8, 3);
        context.MealPlans.Add(new MealPlan
        {
            FamilyGroupId = group.Id,
            Date = date,
            MealType = MealType.Dinner,
        });
        await context.SaveChangesAsync();

        var stored = await context.MealPlans
            .AsNoTracking()
            .SingleAsync(m => m.FamilyGroupId == group.Id && m.Date == date);

        stored.MealType.Should().Be(MealType.Dinner);
        stored.Date.Should().Be(date);
    }

    private static async Task<List<string>> GetTableNamesAsync(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
