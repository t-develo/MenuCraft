using MenuCraft.Api.Dtos.MealPlans;
using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;
using MenuCraft.Api.Repositories;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Services;

public class MealPlanService : IMealPlanService
{
    private readonly IMealPlanRepository _repo;
    private readonly IRecipeRepository _recipeRepo;
    private readonly ILogger<MealPlanService> _logger;

    public MealPlanService(IMealPlanRepository repo, IRecipeRepository recipeRepo, ILogger<MealPlanService> logger)
    {
        _repo = repo;
        _recipeRepo = recipeRepo;
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

    public async Task<WeeklyMealPlanResponse> AutoGenerateAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default)
    {
        ValidateWeekStart(weekStart);

        var recipes = await _recipeRepo.GetByGroupIdAsync(familyGroupId, ct);
        if (recipes.Count == 0)
            throw new InvalidOperationException("レシピが登録されていません。先にレシピを登録してください。");

        await _repo.ClearWeekAsync(familyGroupId, weekStart, ct);

        var shuffled = Shuffle(recipes);
        await AssignRecipesToSlots(familyGroupId, shuffled, slots: GetAllSlots(weekStart), ct);

        _logger.LogInformation("Auto-generated meal plan for group {GroupId}, week {WeekStart}", familyGroupId, weekStart);

        return await GetWeeklyPlansAsync(familyGroupId, weekStart, ct);
    }

    public async Task<WeeklyMealPlanResponse> AutoFillAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default)
    {
        ValidateWeekStart(weekStart);

        var recipes = await _recipeRepo.GetByGroupIdAsync(familyGroupId, ct);
        if (recipes.Count == 0)
            throw new InvalidOperationException("レシピが登録されていません。先にレシピを登録してください。");

        var existingPlans = await _repo.GetWeeklyPlansAsync(familyGroupId, weekStart, ct);
        var filledSlots = existingPlans
            .Where(p => p.MealPlanRecipes.Count > 0)
            .Select(p => (p.Date, p.MealType))
            .ToHashSet();

        var emptySlots = GetAllSlots(weekStart)
            .Where(s => !filledSlots.Contains(s))
            .ToList();

        if (emptySlots.Count > 0)
        {
            var shuffled = Shuffle(recipes);
            await AssignRecipesToSlots(familyGroupId, shuffled, emptySlots, ct);
            _logger.LogInformation(
                "Auto-filled {Count} empty slots for group {GroupId}, week {WeekStart}",
                emptySlots.Count, familyGroupId, weekStart);
        }

        return await GetWeeklyPlansAsync(familyGroupId, weekStart, ct);
    }

    private async Task AssignRecipesToSlots(
        int familyGroupId,
        IReadOnlyList<Recipe> shuffled,
        IReadOnlyList<(DateOnly Date, MealType MealType)> slots,
        CancellationToken ct)
    {
        int index = 0;
        Recipe? previous = null;

        foreach (var (date, mealType) in slots)
        {
            // Pick next recipe using round-robin, avoiding same as previous
            var pickedIndex = index % shuffled.Count;
            if (shuffled.Count > 1 && previous is not null && shuffled[pickedIndex].Id == previous.Id)
                pickedIndex = (pickedIndex + 1) % shuffled.Count;

            var recipe = shuffled[pickedIndex];
            index = (pickedIndex + 1) % shuffled.Count;
            previous = recipe;

            await _repo.CreateOrUpdateAsync(familyGroupId, date, mealType, new[] { recipe.Id }, ct);
        }
    }

    private static IReadOnlyList<Recipe> Shuffle(IReadOnlyList<Recipe> recipes)
    {
        var list = new List<Recipe>(recipes);
        var rng = new Random();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private static IReadOnlyList<(DateOnly Date, MealType MealType)> GetAllSlots(DateOnly weekStart)
    {
        var slots = new List<(DateOnly, MealType)>(14);
        for (var i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            slots.Add((date, MealType.Lunch));
            slots.Add((date, MealType.Dinner));
        }
        return slots;
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
