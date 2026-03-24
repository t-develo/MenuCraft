namespace MenuCraft.Api.Models;

public class MealPlanRecipe
{
    public int Id { get; init; }
    public int MealPlanId { get; init; }
    public int RecipeId { get; init; }

    public MealPlan MealPlan { get; init; } = null!;
    public Recipe Recipe { get; init; } = null!;
}
