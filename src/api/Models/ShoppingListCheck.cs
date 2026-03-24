namespace MenuCraft.Api.Models;

public class ShoppingListCheck
{
    public int Id { get; init; }
    public int FamilyGroupId { get; init; }
    public DateOnly WeekStartDate { get; init; }
    public string IngredientName { get; init; } = string.Empty;
    public bool IsChecked { get; init; }

    public FamilyGroup FamilyGroup { get; init; } = null!;
}
