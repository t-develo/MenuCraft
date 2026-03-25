using System.ComponentModel.DataAnnotations;

namespace MenuCraft.Api.Dtos.Recipes;

public record ParseIngredientsRequest(
    [Required][MaxLength(10000)] string Text
);
