namespace MenuCraft.Api.Dtos.MealPlans;

public record RecipeSummaryResponse(
    int Id,
    string Title,
    string? ImageUrl
);
