using MenuCraft.Api.Dtos.MealPlans;
using MenuCraft.Api.Models.Enums;

namespace MenuCraft.Api.Services;

public interface IMealPlanService
{
    Task<WeeklyMealPlanResponse> GetWeeklyPlansAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default);
    Task<MealPlanResponse> UpdateMealPlanAsync(int familyGroupId, DateOnly date, MealType mealType, UpdateMealPlanRequest request, CancellationToken ct = default);
    Task<WeeklyMealPlanResponse> AutoGenerateAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default);
    Task<WeeklyMealPlanResponse> AutoFillAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default);
}
