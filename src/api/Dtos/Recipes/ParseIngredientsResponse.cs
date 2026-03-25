namespace MenuCraft.Api.Dtos.Recipes;

public record ParseIngredientsResponse(
    IReadOnlyList<IngredientResponse> Ingredients
);
