namespace MenuCraft.Api.Models;

public class RecipeTag
{
    public int Id { get; init; }
    public int RecipeId { get; init; }
    public string Name { get; init; } = string.Empty;

    public Recipe Recipe { get; init; } = null!;
}
