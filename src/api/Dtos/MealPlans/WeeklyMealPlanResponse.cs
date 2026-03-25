namespace MenuCraft.Api.Dtos.MealPlans;

public record WeeklyMealPlanResponse(
    string WeekStart,
    IReadOnlyList<MealPlanResponse> Plans
);
