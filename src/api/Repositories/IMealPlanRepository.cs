using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;

namespace MenuCraft.Api.Repositories;

public interface IMealPlanRepository
{
    Task<IReadOnlyList<MealPlan>> GetWeeklyPlansAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default);
    Task<MealPlan?> GetByDateAndTypeAsync(int familyGroupId, DateOnly date, MealType mealType, CancellationToken ct = default);
    Task<MealPlan> CreateOrUpdateAsync(int familyGroupId, DateOnly date, MealType mealType, IReadOnlyList<int> recipeIds, CancellationToken ct = default);
    Task ClearWeekAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default);
}
