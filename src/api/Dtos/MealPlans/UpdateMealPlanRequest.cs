namespace MenuCraft.Api.Dtos.MealPlans;

public record UpdateMealPlanRequest(
    IReadOnlyList<int> RecipeIds
);
