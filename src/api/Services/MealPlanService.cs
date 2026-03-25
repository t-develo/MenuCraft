using MenuCraft.Api.Dtos.MealPlans;
using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;
using MenuCraft.Api.Repositories;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Services;

public class MealPlanService : IMealPlanService
{
    private readonly IMealPlanRepository _repo;
    private readonly ILogger<MealPlanService> _logger;

    public MealPlanService(IMealPlanRepository repo, ILogger<MealPlanService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<WeeklyMealPlanResponse> GetWeeklyPlansAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default)
    {
        ValidateWeekStart(weekStart);

        var plans = await _repo.GetWeeklyPlansAsync(familyGroupId, weekStart, ct);

        // Build a lookup for quick access
        var planLookup = plans.ToDictionary(
            p => (p.Date, p.MealType),
            p => p
        );

        // Generate all 14 slots (7 days × 2 meal types), including empty ones
        var allPlans = new List<MealPlanResponse>();
        for (var i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            foreach (var mealType in new[] { MealType.Lunch, MealType.Dinner })
            {
                if (planLookup.TryGetValue((date, mealType), out var plan))
                {
                    allPlans.Add(MapToResponse(plan));
                }
                else
                {
                    allPlans.Add(new MealPlanResponse(
                        date.ToString("yyyy-MM-dd"),
                        mealType.ToString(),
                        new List<RecipeSummaryResponse>()
                    ));
                }
            }
        }

        return new WeeklyMealPlanResponse(weekStart.ToString("yyyy-MM-dd"), allPlans);
    }

    public async Task<MealPlanResponse> UpdateMealPlanAsync(int familyGroupId, DateOnly date, MealType mealType, UpdateMealPlanRequest request, CancellationToken ct = default)
    {
        var plan = await _repo.CreateOrUpdateAsync(familyGroupId, date, mealType, request.RecipeIds, ct);
        _logger.LogInformation(
            "MealPlan updated: {Date} {MealType} for group {GroupId} with {Count} recipes",
            date, mealType, familyGroupId, request.RecipeIds.Count);

        return MapToResponse(plan);
    }

    private static void ValidateWeekStart(DateOnly weekStart)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ArgumentException($"週開始日は月曜日である必要があります。指定された日付: {weekStart:yyyy-MM-dd}");
        }
    }

    private static MealPlanResponse MapToResponse(MealPlan plan) => new(
        plan.Date.ToString("yyyy-MM-dd"),
        plan.MealType.ToString(),
        plan.MealPlanRecipes
            .Select(mpr => new RecipeSummaryResponse(mpr.Recipe.Id, mpr.Recipe.Title, mpr.Recipe.ImageUrl))
            .ToList()
    );
}
