using MenuCraft.Api.Models.Enums;

namespace MenuCraft.Api.Models;

public class Recipe
{
    public int Id { get; init; }
    public int FamilyGroupId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Url { get; init; }
    public string? ImageUrl { get; init; }
    public string? Description { get; init; }
    public SourceType SourceType { get; init; } = SourceType.Manual;
    public bool IsDeleted { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    public FamilyGroup FamilyGroup { get; init; } = null!;
    public ICollection<RecipeTag> Tags { get; init; } = new List<RecipeTag>();
    public ICollection<RecipeIngredient> Ingredients { get; init; } = new List<RecipeIngredient>();
    public ICollection<MealPlanRecipe> MealPlanRecipes { get; init; } = new List<MealPlanRecipe>();
}
