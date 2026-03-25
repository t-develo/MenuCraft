namespace MenuCraft.Api.Dtos.Recipes;

public record RecipeResponse(
    int Id,
    string Title,
    string? Url,
    string? ImageUrl,
    string? Description,
    string SourceType,
    IReadOnlyList<string> Tags,
    IReadOnlyList<IngredientResponse> Ingredients,
    DateTime CreatedAt
);

