using System.ComponentModel.DataAnnotations;

namespace MenuCraft.Api.Dtos.Recipes;

public record UpdateRecipeRequest(
    [Required][MaxLength(200)] string Title,
    [Url][MaxLength(2000)] string? Url,
    [MaxLength(2000)] string? ImageUrl,
    [MaxLength(4000)] string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<IngredientRequest> Ingredients
);
