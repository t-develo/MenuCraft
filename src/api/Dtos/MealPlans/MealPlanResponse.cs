namespace MenuCraft.Api.Dtos.MealPlans;

public record MealPlanResponse(
    string Date,
    string MealType,
    IReadOnlyList<RecipeSummaryResponse> Recipes
);
