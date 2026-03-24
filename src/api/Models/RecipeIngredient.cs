namespace MenuCraft.Api.Models;

public class RecipeIngredient
{
    public int Id { get; init; }
    public int RecipeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Quantity { get; init; }
    public string? Unit { get; init; }

    public Recipe Recipe { get; init; } = null!;
}
