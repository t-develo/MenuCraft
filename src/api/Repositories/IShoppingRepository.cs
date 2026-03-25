using MenuCraft.Api.Models;

namespace MenuCraft.Api.Repositories;

public interface IShoppingRepository
{
    Task<IReadOnlyList<MealPlan>> GetWeeklyPlansWithIngredientsAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default);
    Task<IReadOnlyList<ShoppingListCheck>> GetChecksAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default);
    Task UpsertCheckAsync(int familyGroupId, DateOnly weekStart, string ingredientName, bool isChecked, CancellationToken ct = default);
}
