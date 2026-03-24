using MenuCraft.Api.Models.Enums;

namespace MenuCraft.Api.Models;

public class MealPlan
{
    public int Id { get; init; }
    public int FamilyGroupId { get; init; }
    public DateOnly Date { get; init; }
    public MealType MealType { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    public FamilyGroup FamilyGroup { get; init; } = null!;
    public ICollection<MealPlanRecipe> MealPlanRecipes { get; init; } = new List<MealPlanRecipe>();
}
