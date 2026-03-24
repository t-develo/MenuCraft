namespace MenuCraft.Api.Models;

public class FamilyGroup
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string InviteCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public ICollection<User> Members { get; init; } = new List<User>();
    public ICollection<Recipe> Recipes { get; init; } = new List<Recipe>();
    public ICollection<MealPlan> MealPlans { get; init; } = new List<MealPlan>();
    public ICollection<ShoppingListCheck> ShoppingListChecks { get; init; } = new List<ShoppingListCheck>();
}
