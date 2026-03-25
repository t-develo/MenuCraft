namespace MenuCraft.Api.Dtos.Recipes;

public record IngredientResponse(
    string Name,
    string? Quantity,
    string? Unit
);
