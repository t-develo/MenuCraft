using FluentAssertions;
using MenuCraft.Api.Dtos.Shopping;
using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuCraft.Api.Tests.Services;

public class ShoppingServiceTests
{
    private readonly Mock<IShoppingRepository> _mockRepo;
    private readonly ShoppingService _sut;

    public ShoppingServiceTests()
    {
        _mockRepo = new Mock<IShoppingRepository>();
        var logger = Mock.Of<ILogger<ShoppingService>>();
        _sut = new ShoppingService(_mockRepo.Object, logger);
    }

    // ── GetShoppingListAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetShoppingListAsync_WithNoMealPlans_ReturnsEmptyList()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1);
        _mockRepo.Setup(r => r.GetWeeklyPlansWithIngredientsAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MealPlan>());
        _mockRepo.Setup(r => r.GetChecksAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShoppingListCheck>());

        // Act
        var result = await _sut.GetShoppingListAsync(42, weekStart);

        // Assert
        result.WeekStart.Should().Be("2024-01-01");
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetShoppingListAsync_AggregatesSameIngredientAcrossRecipes()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1);
        var plans = CreatePlansWithIngredients(new[]
        {
            ("カレー", new[] { ("玉ねぎ", "1", "個"), ("にんじん", "2", "本") }),
            ("シチュー", new[] { ("玉ねぎ", "1", "個"), ("じゃがいも", "3", "個") }),
        });

        _mockRepo.Setup(r => r.GetWeeklyPlansWithIngredientsAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);
        _mockRepo.Setup(r => r.GetChecksAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShoppingListCheck>());

        // Act
        var result = await _sut.GetShoppingListAsync(42, weekStart);

        // Assert
        result.Items.Should().HaveCount(3); // 玉ねぎ, にんじん, じゃがいも
        var onion = result.Items.First(i => i.IngredientName == "玉ねぎ");
        onion.TotalQuantity.Should().Be("2");
        onion.Unit.Should().Be("個");
        onion.Sources.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetShoppingListAsync_SameIngredientDifferentUnit_DoesNotSum()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1);
        var plans = CreatePlansWithIngredients(new[]
        {
            ("レシピA", new[] { ("牛乳", "200", "ml") }),
            ("レシピB", new[] { ("牛乳", "1", "カップ") }),
        });

        _mockRepo.Setup(r => r.GetWeeklyPlansWithIngredientsAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);
        _mockRepo.Setup(r => r.GetChecksAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShoppingListCheck>());

        // Act
        var result = await _sut.GetShoppingListAsync(42, weekStart);

        // Assert
        var milk = result.Items.First(i => i.IngredientName == "牛乳");
        milk.TotalQuantity.Should().BeNull(); // cannot sum different units
        milk.Sources.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetShoppingListAsync_NonNumericQuantity_DoesNotSum()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1);
        var plans = CreatePlansWithIngredients(new[]
        {
            ("レシピA", new[] { ("塩", "少々", (string?)null) }),
            ("レシピB", new[] { ("塩", "少々", (string?)null) }),
        });

        _mockRepo.Setup(r => r.GetWeeklyPlansWithIngredientsAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);
        _mockRepo.Setup(r => r.GetChecksAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShoppingListCheck>());

        // Act
        var result = await _sut.GetShoppingListAsync(42, weekStart);

        // Assert
        var salt = result.Items.First(i => i.IngredientName == "塩");
        salt.TotalQuantity.Should().BeNull();
        salt.Sources.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetShoppingListAsync_MergesCheckState()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1);
        var plans = CreatePlansWithIngredients(new[]
        {
            ("カレー", new[] { ("玉ねぎ", "1", "個"), ("にんじん", "2", "本") }),
        });

        var checks = new List<ShoppingListCheck>
        {
            new() { FamilyGroupId = 42, WeekStartDate = weekStart, IngredientName = "玉ねぎ", IsChecked = true }
        };

        _mockRepo.Setup(r => r.GetWeeklyPlansWithIngredientsAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);
        _mockRepo.Setup(r => r.GetChecksAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checks);

        // Act
        var result = await _sut.GetShoppingListAsync(42, weekStart);

        // Assert
        result.Items.First(i => i.IngredientName == "玉ねぎ").IsChecked.Should().BeTrue();
        result.Items.First(i => i.IngredientName == "にんじん").IsChecked.Should().BeFalse();
    }

    [Fact]
    public async Task GetShoppingListAsync_ItemsAreSortedAlphabetically()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1);
        var plans = CreatePlansWithIngredients(new[]
        {
            ("レシピ", new[] { ("玉ねぎ", "1", "個"), ("あぶらあげ", "1", "枚"), ("にんじん", "2", "本") }),
        });

        _mockRepo.Setup(r => r.GetWeeklyPlansWithIngredientsAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);
        _mockRepo.Setup(r => r.GetChecksAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShoppingListCheck>());

        // Act
        var result = await _sut.GetShoppingListAsync(42, weekStart);

        // Assert
        var names = result.Items.Select(i => i.IngredientName).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetShoppingListAsync_SingleIngredient_ReturnsTotalQuantityAndUnit()
    {
        // Arrange
        var weekStart = new DateOnly(2024, 1, 1);
        var plans = CreatePlansWithIngredients(new[]
        {
            ("パスタ", new[] { ("パスタ", "100", "g") }),
        });

        _mockRepo.Setup(r => r.GetWeeklyPlansWithIngredientsAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);
        _mockRepo.Setup(r => r.GetChecksAsync(42, weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShoppingListCheck>());

        // Act
        var result = await _sut.GetShoppingListAsync(42, weekStart);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.TotalQuantity.Should().Be("100");
        item.Unit.Should().Be("g");
    }

    // ── UpdateCheckAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCheckAsync_WithValidRequest_CallsRepository()
    {
        // Arrange
        var request = new UpdateCheckRequest("2024-01-01", "玉ねぎ", true);
        _mockRepo.Setup(r => r.UpsertCheckAsync(42, new DateOnly(2024, 1, 1), "玉ねぎ", true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateCheckAsync(42, request);

        // Assert
        _mockRepo.Verify(r => r.UpsertCheckAsync(
            42, new DateOnly(2024, 1, 1), "玉ねぎ", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCheckAsync_WithInvalidWeekStart_ThrowsArgumentException()
    {
        // Arrange
        var request = new UpdateCheckRequest("not-a-date", "玉ねぎ", true);

        // Act
        var act = () => _sut.UpdateCheckAsync(42, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*weekStart*");
    }

    [Fact]
    public async Task UpdateCheckAsync_WithEmptyIngredientName_ThrowsArgumentException()
    {
        // Arrange
        var request = new UpdateCheckRequest("2024-01-01", "", true);

        // Act
        var act = () => _sut.UpdateCheckAsync(42, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ingredientName*");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IReadOnlyList<MealPlan> CreatePlansWithIngredients(
        IEnumerable<(string RecipeTitle, (string Name, string? Qty, string? Unit)[] Ingredients)> recipes)
    {
        var plans = new List<MealPlan>();
        var recipeId = 1;
        var planId = 1;

        foreach (var (title, ingredients) in recipes)
        {
            var recipe = new Recipe
            {
                Id = recipeId++,
                Title = title,
                FamilyGroupId = 42,
                Ingredients = ingredients.Select((ing, idx) => new RecipeIngredient
                {
                    Id = idx + 1,
                    Name = ing.Name,
                    Quantity = ing.Qty,
                    Unit = ing.Unit
                }).ToList()
            };

            plans.Add(new MealPlan
            {
                Id = planId,
                FamilyGroupId = 42,
                Date = new DateOnly(2024, 1, planId),
                MealType = MealType.Lunch,
                MealPlanRecipes = new List<MealPlanRecipe>
                {
                    new() { Id = planId, MealPlanId = planId, RecipeId = recipe.Id, Recipe = recipe }
                }
            });
            planId++;
        }

        return plans;
    }
}
