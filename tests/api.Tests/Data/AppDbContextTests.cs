using FluentAssertions;
using MenuCraft.Api.Data;
using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuCraft.Api.Tests.Data;

public class AppDbContextTests : IDisposable
{
    private readonly AppDbContext _context;

    public AppDbContextTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
    }

    [Fact]
    public async Task CanCreateFamilyGroupWithMembers()
    {
        // Arrange
        var group = new FamilyGroup
        {
            Name = "テスト家族",
            InviteCode = "ABC123"
        };
        _context.FamilyGroups.Add(group);
        await _context.SaveChangesAsync();

        var user = new User
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            FamilyGroupId = group.Id
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var savedGroup = await _context.FamilyGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == group.Id);

        // Assert
        savedGroup.Should().NotBeNull();
        savedGroup!.Name.Should().Be("テスト家族");
        savedGroup.Members.Should().HaveCount(1);
    }

    [Fact]
    public async Task CanCreateRecipeWithTagsAndIngredients()
    {
        // Arrange
        var group = new FamilyGroup { Name = "家族", InviteCode = "XYZ789" };
        _context.FamilyGroups.Add(group);
        await _context.SaveChangesAsync();

        var recipe = new Recipe
        {
            FamilyGroupId = group.Id,
            Title = "カレーライス",
            Url = "https://example.com/curry",
            SourceType = SourceType.Web,
            Tags = new List<RecipeTag>
            {
                new() { Name = "和食" },
                new() { Name = "定番" }
            },
            Ingredients = new List<RecipeIngredient>
            {
                new() { Name = "鶏もも肉", Quantity = "300", Unit = "g" },
                new() { Name = "にんじん", Quantity = "1", Unit = "本" },
                new() { Name = "じゃがいも", Quantity = "2", Unit = "個" }
            }
        };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        // Act
        var savedRecipe = await _context.Recipes
            .Include(r => r.Tags)
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == recipe.Id);

        // Assert
        savedRecipe.Should().NotBeNull();
        savedRecipe!.Title.Should().Be("カレーライス");
        savedRecipe.Tags.Should().HaveCount(2);
        savedRecipe.Ingredients.Should().HaveCount(3);
        savedRecipe.SourceType.Should().Be(SourceType.Web);
    }

    [Fact]
    public async Task CanCreateMealPlanWithRecipes()
    {
        // Arrange
        var group = new FamilyGroup { Name = "家族", InviteCode = "MP1234" };
        _context.FamilyGroups.Add(group);
        await _context.SaveChangesAsync();

        var recipe1 = new Recipe { FamilyGroupId = group.Id, Title = "カレー" };
        var recipe2 = new Recipe { FamilyGroupId = group.Id, Title = "パスタ" };
        _context.Recipes.AddRange(recipe1, recipe2);
        await _context.SaveChangesAsync();

        var mealPlan = new MealPlan
        {
            FamilyGroupId = group.Id,
            Date = new DateOnly(2026, 3, 23),
            MealType = MealType.Dinner,
            MealPlanRecipes = new List<MealPlanRecipe>
            {
                new() { RecipeId = recipe1.Id },
                new() { RecipeId = recipe2.Id }
            }
        };
        _context.MealPlans.Add(mealPlan);
        await _context.SaveChangesAsync();

        // Act
        var savedPlan = await _context.MealPlans
            .Include(m => m.MealPlanRecipes)
                .ThenInclude(mpr => mpr.Recipe)
            .FirstOrDefaultAsync(m => m.Id == mealPlan.Id);

        // Assert
        savedPlan.Should().NotBeNull();
        savedPlan!.Date.Should().Be(new DateOnly(2026, 3, 23));
        savedPlan.MealType.Should().Be(MealType.Dinner);
        savedPlan.MealPlanRecipes.Should().HaveCount(2);
    }

    [Fact]
    public async Task CanCreateShoppingListCheck()
    {
        // Arrange
        var group = new FamilyGroup { Name = "家族", InviteCode = "SL5678" };
        _context.FamilyGroups.Add(group);
        await _context.SaveChangesAsync();

        var check = new ShoppingListCheck
        {
            FamilyGroupId = group.Id,
            WeekStartDate = new DateOnly(2026, 3, 23),
            IngredientName = "にんじん",
            IsChecked = true
        };
        _context.ShoppingListChecks.Add(check);
        await _context.SaveChangesAsync();

        // Act
        var savedCheck = await _context.ShoppingListChecks
            .FirstOrDefaultAsync(s => s.Id == check.Id);

        // Assert
        savedCheck.Should().NotBeNull();
        savedCheck!.IngredientName.Should().Be("にんじん");
        savedCheck.IsChecked.Should().BeTrue();
    }

    [Fact]
    public async Task DeletingRecipe_RemovesTagsAndIngredients()
    {
        // Arrange
        var group = new FamilyGroup { Name = "家族", InviteCode = "DEL001" };
        _context.FamilyGroups.Add(group);
        await _context.SaveChangesAsync();

        var recipe = new Recipe
        {
            FamilyGroupId = group.Id,
            Title = "削除テスト",
            Tags = new List<RecipeTag> { new() { Name = "テスト" } },
            Ingredients = new List<RecipeIngredient> { new() { Name = "塩" } }
        };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        var recipeId = recipe.Id;

        // Act
        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();

        // Assert
        var tags = await _context.RecipeTags.Where(t => t.RecipeId == recipeId).ToListAsync();
        var ingredients = await _context.RecipeIngredients.Where(i => i.RecipeId == recipeId).ToListAsync();
        tags.Should().BeEmpty();
        ingredients.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletingFamilyGroup_CascadesDeleteToRecipes()
    {
        // Arrange
        var group = new FamilyGroup { Name = "家族", InviteCode = "CAS001" };
        _context.FamilyGroups.Add(group);
        await _context.SaveChangesAsync();

        var recipe = new Recipe { FamilyGroupId = group.Id, Title = "カスケードテスト" };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        var groupId = group.Id;

        // Act
        _context.FamilyGroups.Remove(group);
        await _context.SaveChangesAsync();

        // Assert
        var recipes = await _context.Recipes.Where(r => r.FamilyGroupId == groupId).ToListAsync();
        recipes.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
