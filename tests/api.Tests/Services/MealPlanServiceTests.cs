using FluentAssertions;
using MenuCraft.Api.Dtos.MealPlans;
using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuCraft.Api.Tests.Services;

public class MealPlanServiceTests
{
    private readonly Mock<IMealPlanRepository> _mockRepo;
    private readonly Mock<IRecipeRepository> _mockRecipeRepo;
    private readonly MealPlanService _sut;

    public MealPlanServiceTests()
    {
        _mockRepo = new Mock<IMealPlanRepository>();
        _mockRecipeRepo = new Mock<IRecipeRepository>();
        var logger = Mock.Of<ILogger<MealPlanService>>();
        _sut = new MealPlanService(_mockRepo.Object, _mockRecipeRepo.Object, logger);
    }

    // ── GetWeeklyPlansAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetWeeklyPlansAsync_ReturnsFull14Slots()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1); // Monday
        _mockRepo.Setup(r => r.GetWeeklyPlansAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MealPlan>());

        // Act
        var result = await _sut.GetWeeklyPlansAsync(42, weekStart);

        // Assert
        result.WeekStart.Should().Be("2024-01-01");
        result.Plans.Should().HaveCount(14);
    }

    [Fact]
    public async Task GetWeeklyPlansAsync_WithExistingPlans_ReturnsRecipes()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1); // Monday
        var recipe = new Recipe { Id = 10, Title = "カレー", FamilyGroupId = 42 };
        var plans = new List<MealPlan>
        {
            new()
            {
                Id = 1,
                FamilyGroupId = 42,
                Date = weekStart,
                MealType = MealType.Lunch,
                MealPlanRecipes = new List<MealPlanRecipe>
                {
                    new() { Id = 1, MealPlanId = 1, RecipeId = 10, Recipe = recipe }
                }
            }
        };

        _mockRepo.Setup(r => r.GetWeeklyPlansAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        // Act
        var result = await _sut.GetWeeklyPlansAsync(42, weekStart);

        // Assert
        result.Plans.Should().HaveCount(14);

        var lunchOnMonday = result.Plans.First(p => p.Date == "2024-01-01" && p.MealType == "Lunch");
        lunchOnMonday.Recipes.Should().HaveCount(1);
        lunchOnMonday.Recipes[0].Id.Should().Be(10);
        lunchOnMonday.Recipes[0].Title.Should().Be("カレー");

        // All other slots should be empty
        var emptySlots = result.Plans.Where(p => !(p.Date == "2024-01-01" && p.MealType == "Lunch"));
        emptySlots.Should().AllSatisfy(p => p.Recipes.Should().BeEmpty());
    }

    [Fact]
    public async Task GetWeeklyPlansAsync_SlotsAreOrderedByDateThenMealType()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1); // Monday
        _mockRepo.Setup(r => r.GetWeeklyPlansAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MealPlan>());

        // Act
        var result = await _sut.GetWeeklyPlansAsync(42, weekStart);

        // Assert
        // First slot: Monday Lunch
        result.Plans[0].Date.Should().Be("2024-01-01");
        result.Plans[0].MealType.Should().Be("Lunch");
        // Second slot: Monday Dinner
        result.Plans[1].Date.Should().Be("2024-01-01");
        result.Plans[1].MealType.Should().Be("Dinner");
        // Last slot: Sunday Dinner
        result.Plans[13].Date.Should().Be("2024-01-07");
        result.Plans[13].MealType.Should().Be("Dinner");
    }

    [Fact]
    public async Task GetWeeklyPlansAsync_WithNonMonday_ThrowsArgumentException()
    {
        // Arrange
        var notMonday = new DateOnly(2024, 1, 2); // Tuesday

        // Act
        var act = () => _sut.GetWeeklyPlansAsync(42, notMonday);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*月曜日*");
    }

    // ── UpdateMealPlanAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMealPlanAsync_WithValidData_ReturnsMealPlanResponse()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 1);
        var recipe = new Recipe { Id = 5, Title = "パスタ", FamilyGroupId = 42 };
        var updatedPlan = new MealPlan
        {
            Id = 1,
            FamilyGroupId = 42,
            Date = date,
            MealType = MealType.Dinner,
            MealPlanRecipes = new List<MealPlanRecipe>
            {
                new() { Id = 1, MealPlanId = 1, RecipeId = 5, Recipe = recipe }
            }
        };

        _mockRepo.Setup(r => r.CreateOrUpdateAsync(42, date, MealType.Dinner, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedPlan);

        var request = new UpdateMealPlanRequest(new List<int> { 5 });

        // Act
        var result = await _sut.UpdateMealPlanAsync(42, date, MealType.Dinner, request);

        // Assert
        result.Should().NotBeNull();
        result.Date.Should().Be("2024-01-01");
        result.MealType.Should().Be("Dinner");
        result.Recipes.Should().HaveCount(1);
        result.Recipes[0].Id.Should().Be(5);
        result.Recipes[0].Title.Should().Be("パスタ");
    }

    [Fact]
    public async Task UpdateMealPlanAsync_WithEmptyRecipeIds_ClearsRecipes()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 1);
        var emptyPlan = new MealPlan
        {
            Id = 1,
            FamilyGroupId = 42,
            Date = date,
            MealType = MealType.Lunch,
            MealPlanRecipes = new List<MealPlanRecipe>()
        };

        _mockRepo.Setup(r => r.CreateOrUpdateAsync(42, date, MealType.Lunch, It.Is<IReadOnlyList<int>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPlan);

        var request = new UpdateMealPlanRequest(new List<int>());

        // Act
        var result = await _sut.UpdateMealPlanAsync(42, date, MealType.Lunch, request);

        // Assert
        result.Recipes.Should().BeEmpty();
        _mockRepo.Verify(r => r.CreateOrUpdateAsync(
            42, date, MealType.Lunch,
            It.Is<IReadOnlyList<int>>(ids => ids.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateMealPlanAsync_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var date = new DateOnly(2024, 3, 18); // Monday
        var recipeIds = new List<int> { 1, 2, 3 };
        var plan = new MealPlan
        {
            Id = 1,
            FamilyGroupId = 10,
            Date = date,
            MealType = MealType.Lunch,
            MealPlanRecipes = new List<MealPlanRecipe>()
        };

        _mockRepo.Setup(r => r.CreateOrUpdateAsync(
            10, date, MealType.Lunch, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var request = new UpdateMealPlanRequest(recipeIds);

        // Act
        await _sut.UpdateMealPlanAsync(10, date, MealType.Lunch, request);

        // Assert
        _mockRepo.Verify(r => r.CreateOrUpdateAsync(
            10, date, MealType.Lunch,
            It.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(recipeIds)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── AutoGenerateAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task AutoGenerateAsync_WithNoRecipes_ThrowsInvalidOperationException()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1); // Monday
        _mockRecipeRepo.Setup(r => r.GetByGroupIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Recipe>());

        // Act
        var act = () => _sut.AutoGenerateAsync(42, weekStart);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*レシピが登録されていません*");
    }

    [Fact]
    public async Task AutoGenerateAsync_WithRecipes_FillsAll14Slots()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1); // Monday
        var recipes = CreateRecipeList(3, groupId: 42);

        _mockRecipeRepo.Setup(r => r.GetByGroupIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipes);
        _mockRepo.Setup(r => r.ClearWeekAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupCreateOrUpdateForAllSlots(weekStart);
        SetupGetWeeklyPlans(42, weekStart, recipes);

        // Act
        var result = await _sut.AutoGenerateAsync(42, weekStart);

        // Assert
        result.Should().NotBeNull();
        result.WeekStart.Should().Be("2024-01-01");
        result.Plans.Should().HaveCount(14);

        // ClearWeek must be called before creating slots
        _mockRepo.Verify(r => r.ClearWeekAsync(42, weekStart, It.IsAny<CancellationToken>()), Times.Once);
        // CreateOrUpdate called 14 times (once per slot)
        _mockRepo.Verify(r => r.CreateOrUpdateAsync(
            42, It.IsAny<DateOnly>(), It.IsAny<MealType>(),
            It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Exactly(14));
    }

    [Fact]
    public async Task AutoGenerateAsync_WithNonMonday_ThrowsArgumentException()
    {
        // Arrange
        var notMonday = new DateOnly(2024, 1, 2); // Tuesday

        // Act
        var act = () => _sut.AutoGenerateAsync(42, notMonday);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*月曜日*");
    }

    [Fact]
    public async Task AutoGenerateAsync_WithSingleRecipe_DoesNotThrow()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1);
        var recipes = CreateRecipeList(1, groupId: 42);

        _mockRecipeRepo.Setup(r => r.GetByGroupIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipes);
        _mockRepo.Setup(r => r.ClearWeekAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetupCreateOrUpdateForAllSlots(weekStart);
        SetupGetWeeklyPlans(42, weekStart, recipes);

        // Act
        var act = () => _sut.AutoGenerateAsync(42, weekStart);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ── AutoFillAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task AutoFillAsync_WithNoRecipes_ThrowsInvalidOperationException()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1); // Monday
        _mockRecipeRepo.Setup(r => r.GetByGroupIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Recipe>());

        // Act
        var act = () => _sut.AutoFillAsync(42, weekStart);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*レシピが登録されていません*");
    }

    [Fact]
    public async Task AutoFillAsync_PreservesExistingSlots()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1); // Monday
        var recipe = new Recipe { Id = 99, Title = "既存レシピ", FamilyGroupId = 42 };
        var recipes = CreateRecipeList(3, groupId: 42);

        // One slot already filled (Monday Lunch)
        var existingPlans = new List<MealPlan>
        {
            new()
            {
                Id = 1,
                FamilyGroupId = 42,
                Date = weekStart,
                MealType = MealType.Lunch,
                MealPlanRecipes = new List<MealPlanRecipe>
                {
                    new() { Id = 1, MealPlanId = 1, RecipeId = 99, Recipe = recipe }
                }
            }
        };

        _mockRecipeRepo.Setup(r => r.GetByGroupIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipes);
        _mockRepo.Setup(r => r.GetWeeklyPlansAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlans);
        SetupCreateOrUpdateForAllSlots(weekStart);
        SetupGetWeeklyPlansWithExisting(42, weekStart, recipes, existingPlans);

        // Act
        await _sut.AutoFillAsync(42, weekStart);

        // Assert
        // Should NOT overwrite the already-filled Monday Lunch slot
        _mockRepo.Verify(r => r.CreateOrUpdateAsync(
            42, weekStart, MealType.Lunch,
            It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Never);

        // Should fill the other 13 empty slots
        _mockRepo.Verify(r => r.CreateOrUpdateAsync(
            42, It.IsAny<DateOnly>(), It.IsAny<MealType>(),
            It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Exactly(13));
    }

    [Fact]
    public async Task AutoFillAsync_WhenAllSlotsFilled_DoesNotCallCreateOrUpdate()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1);
        var recipes = CreateRecipeList(2, groupId: 42);

        // Build all 14 slots filled
        var allPlans = new List<MealPlan>();
        for (var i = 0; i < 7; i++)
        {
            foreach (var mt in new[] { MealType.Lunch, MealType.Dinner })
            {
                var r = recipes[0];
                allPlans.Add(new MealPlan
                {
                    Id = allPlans.Count + 1,
                    FamilyGroupId = 42,
                    Date = weekStart.AddDays(i),
                    MealType = mt,
                    MealPlanRecipes = new List<MealPlanRecipe>
                    {
                        new() { RecipeId = r.Id, Recipe = r }
                    }
                });
            }
        }

        _mockRecipeRepo.Setup(r => r.GetByGroupIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipes);
        _mockRepo.Setup(r => r.GetWeeklyPlansAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPlans);
        SetupGetWeeklyPlansWithExisting(42, weekStart, recipes, allPlans);

        // Act
        await _sut.AutoFillAsync(42, weekStart);

        // Assert: no new slots created
        _mockRepo.Verify(r => r.CreateOrUpdateAsync(
            It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<MealType>(),
            It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<Recipe> CreateRecipeList(int count, int groupId) =>
        Enumerable.Range(1, count)
            .Select(i => new Recipe { Id = i, Title = $"レシピ{i}", FamilyGroupId = groupId })
            .ToList();

    private void SetupCreateOrUpdateForAllSlots(DateOnly weekStart)
    {
        for (var i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            foreach (var mt in new[] { MealType.Lunch, MealType.Dinner })
            {
                var d = date;
                var m = mt;
                _mockRepo.Setup(r => r.CreateOrUpdateAsync(
                        42, d, m, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new MealPlan
                    {
                        Id = i * 2 + (mt == MealType.Lunch ? 1 : 2),
                        FamilyGroupId = 42,
                        Date = d,
                        MealType = m,
                        MealPlanRecipes = new List<MealPlanRecipe>()
                    });
            }
        }
    }

    private void SetupGetWeeklyPlans(int groupId, DateOnly weekStart, IReadOnlyList<Recipe> recipes)
    {
        _mockRepo.Setup(r => r.GetWeeklyPlansAsync(groupId, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MealPlan>());
    }

    private void SetupGetWeeklyPlansWithExisting(
        int groupId, DateOnly weekStart,
        IReadOnlyList<Recipe> recipes, IReadOnlyList<MealPlan> existing)
    {
        _mockRepo.Setup(r => r.GetWeeklyPlansAsync(groupId, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing.ToList());
    }
}
