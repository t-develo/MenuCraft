using System.ComponentModel.DataAnnotations;

namespace MenuCraft.Api.Dtos.Recipes;

public record CreateRecipeRequest(
    [Required][MaxLength(200)] string Title,
    [Url][MaxLength(2000)] string? Url,
    [MaxLength(2000)] string? ImageUrl,
    [MaxLength(4000)] string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<IngredientRequest> Ingredients
);

public record IngredientRequest(
    [Required][MaxLength(100)] string Name,
    [MaxLength(50)] string? Quantity,
    [MaxLength(30)] string? Unit
);
