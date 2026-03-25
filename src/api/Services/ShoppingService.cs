using MenuCraft.Api.Dtos.Shopping;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Services;

public class ShoppingService : IShoppingService
{
    private readonly IShoppingRepository _repo;
    private readonly ILogger<ShoppingService> _logger;

    public ShoppingService(IShoppingRepository repo, ILogger<ShoppingService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<ShoppingListResponse> GetShoppingListAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default)
    {
        var plans = await _repo.GetWeeklyPlansWithIngredientsAsync(familyGroupId, weekStart, ct);
        var checks = await _repo.GetChecksAsync(familyGroupId, weekStart, ct);

        var checkLookup = checks.ToDictionary(c => c.IngredientName, c => c.IsChecked);

        var items = AggregateIngredients(plans, checkLookup);

        return new ShoppingListResponse(weekStart.ToString("yyyy-MM-dd"), items);
    }

    public async Task UpdateCheckAsync(int familyGroupId, UpdateCheckRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.WeekStart) ||
            !DateOnly.TryParse(request.WeekStart, out var weekStart))
        {
            throw new ArgumentException($"weekStart の形式が正しくありません: {request.WeekStart}");
        }

        if (string.IsNullOrWhiteSpace(request.IngredientName))
        {
            throw new ArgumentException("ingredientName は必須です");
        }

        await _repo.UpsertCheckAsync(familyGroupId, weekStart, request.IngredientName, request.IsChecked, ct);

        _logger.LogInformation(
            "Shopping check updated: group={GroupId}, week={WeekStart}, ingredient={Name}, checked={IsChecked}",
            familyGroupId, weekStart, request.IngredientName, request.IsChecked);
    }

    private static IReadOnlyList<ShoppingItemResponse> AggregateIngredients(
        IReadOnlyList<MealPlan> plans,
        IReadOnlyDictionary<string, bool> checkLookup)
    {
        // Collect all ingredient occurrences with their recipe context
        var occurrences = new List<(string Name, string? Quantity, string? Unit, string RecipeName)>();

        foreach (var plan in plans)
        {
            foreach (var mpr in plan.MealPlanRecipes)
            {
                var recipe = mpr.Recipe;
                foreach (var ingredient in recipe.Ingredients)
                {
                    var normalizedName = ingredient.Name.Trim();
                    occurrences.Add((normalizedName, ingredient.Quantity, ingredient.Unit, recipe.Title));
                }
            }
        }

        // Group by ingredient name
        var groups = occurrences.GroupBy(o => o.Name);

        var items = new List<ShoppingItemResponse>();

        foreach (var group in groups.OrderBy(g => g.Key))
        {
            var name = group.Key;
            var entries = group.ToList();

            var sources = entries
                .Select(e => new ShoppingItemSource(e.RecipeName, e.Quantity, e.Unit))
                .ToList();

            var (totalQuantity, unit) = TryAggregateQuantity(entries);
            var isChecked = checkLookup.TryGetValue(name, out var val) && val;

            items.Add(new ShoppingItemResponse(name, totalQuantity, unit, isChecked, sources));
        }

        return items;
    }

    /// <summary>
    /// If all entries share the same non-null unit and all quantities are numeric, sum them.
    /// Otherwise return null for totalQuantity (callers should display sources individually).
    /// </summary>
    private static (string? TotalQuantity, string? Unit) TryAggregateQuantity(
        IReadOnlyList<(string Name, string? Quantity, string? Unit, string RecipeName)> entries)
    {
        if (entries.Count == 0)
            return (null, null);

        if (entries.Count == 1)
            return (entries[0].Quantity, entries[0].Unit);

        // Check all units are the same (non-null)
        var firstUnit = entries[0].Unit;
        if (firstUnit is null || entries.Any(e => e.Unit != firstUnit))
            return (null, firstUnit);

        // Check all quantities are numeric
        var sum = 0.0;
        foreach (var entry in entries)
        {
            if (!double.TryParse(entry.Quantity, out var qty))
                return (null, firstUnit);
            sum += qty;
        }

        // Format without unnecessary decimal point
        var formatted = sum == Math.Floor(sum)
            ? ((int)sum).ToString()
            : sum.ToString("G");

        return (formatted, firstUnit);
    }
}
