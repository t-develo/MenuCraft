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
    private readonly MealPlanService _sut;

    public MealPlanServiceTests()
    {
        _mockRepo = new Mock<IMealPlanRepository>();
        var logger = Mock.Of<ILogger<MealPlanService>>();
        _sut = new MealPlanService(_mockRepo.Object, logger);
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
}
