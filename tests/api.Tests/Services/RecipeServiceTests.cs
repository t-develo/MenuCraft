using FluentAssertions;
using MenuCraft.Api.Dtos.Recipes;
using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuCraft.Api.Tests.Services;

public class RecipeServiceTests
{
    private readonly Mock<IRecipeRepository> _mockRepo;
    private readonly RecipeService _sut;

    public RecipeServiceTests()
    {
        _mockRepo = new Mock<IRecipeRepository>();
        var logger = Mock.Of<ILogger<RecipeService>>();
        _sut = new RecipeService(_mockRepo.Object, logger);
    }

    [Fact]
    public async Task GetRecipeAsync_WithValidId_ReturnsRecipe()
    {
        // Arrange
        var recipe = new Recipe
        {
            Id = 1,
            FamilyGroupId = 42,
            Title = "カレーライス",
            SourceType = SourceType.Manual,
            Tags = new List<RecipeTag> { new() { Name = "和食" } },
            Ingredients = new List<RecipeIngredient>
            {
                new() { Name = "鶏肉", Quantity = "300", Unit = "g" }
            }
        };
        _mockRepo.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);

        // Act
        var result = await _sut.GetRecipeAsync(1, familyGroupId: 42);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("カレーライス");
        result.Tags.Should().Contain("和食");
        result.Ingredients.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRecipeAsync_WrongGroup_ReturnsNull()
    {
        // Arrange
        var recipe = new Recipe { Id = 1, FamilyGroupId = 42, Title = "カレー" };
        _mockRepo.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);

        // Act
        var result = await _sut.GetRecipeAsync(1, familyGroupId: 99);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecipesAsync_ReturnsGroupRecipes()
    {
        // Arrange
        var recipes = new List<Recipe>
        {
            new() { Id = 1, FamilyGroupId = 42, Title = "カレー", Tags = new List<RecipeTag>(), Ingredients = new List<RecipeIngredient>() },
            new() { Id = 2, FamilyGroupId = 42, Title = "パスタ", Tags = new List<RecipeTag>(), Ingredients = new List<RecipeIngredient>() }
        };
        _mockRepo.Setup(r => r.GetByGroupIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipes);

        // Act
        var result = await _sut.GetRecipesAsync(42);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateRecipeAsync_CreatesAndReturnsRecipe()
    {
        // Arrange
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe r, CancellationToken _) =>
            {
                return new Recipe
                {
                    Id = 1,
                    FamilyGroupId = r.FamilyGroupId,
                    Title = r.Title,
                    Url = r.Url,
                    SourceType = r.SourceType,
                    Tags = r.Tags,
                    Ingredients = r.Ingredients
                };
            });

        var request = new CreateRecipeRequest(
            "カレーライス",
            "https://example.com/curry",
            null,
            "おいしいカレー",
            new List<string> { "和食" },
            new List<IngredientRequest>
            {
                new("鶏肉", "300", "g")
            }
        );

        // Act
        var result = await _sut.CreateRecipeAsync(42, request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("カレーライス");
        result.SourceType.Should().Be("Web");
        result.Tags.Should().Contain("和食");
    }

    [Fact]
    public async Task CreateRecipeAsync_WithYouTubeUrl_SetsYouTubeSourceType()
    {
        // Arrange
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe r, CancellationToken _) => new Recipe
            {
                Id = 1,
                FamilyGroupId = r.FamilyGroupId,
                Title = r.Title,
                Url = r.Url,
                SourceType = r.SourceType,
                Tags = r.Tags,
                Ingredients = r.Ingredients
            });

        var request = new CreateRecipeRequest(
            "テスト",
            "https://www.youtube.com/watch?v=abc123",
            null, null,
            new List<string>(),
            new List<IngredientRequest>()
        );

        // Act
        var result = await _sut.CreateRecipeAsync(42, request);

        // Assert
        result.SourceType.Should().Be("YouTube");
    }

    [Fact]
    public async Task DeleteRecipeAsync_WrongGroup_ReturnsFalse()
    {
        // Arrange
        var recipe = new Recipe { Id = 1, FamilyGroupId = 42, Title = "カレー" };
        _mockRepo.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);

        // Act
        var result = await _sut.DeleteRecipeAsync(1, familyGroupId: 99);

        // Assert
        result.Should().BeFalse();
        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRecipeAsync_ValidRecipe_ReturnsTrue()
    {
        // Arrange
        var recipe = new Recipe { Id = 1, FamilyGroupId = 42, Title = "カレー" };
        _mockRepo.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);

        // Act
        var result = await _sut.DeleteRecipeAsync(1, familyGroupId: 42);

        // Assert
        result.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
