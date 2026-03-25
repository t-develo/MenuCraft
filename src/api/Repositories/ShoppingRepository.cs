using MenuCraft.Api.Data;
using MenuCraft.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuCraft.Api.Repositories;

public class ShoppingRepository : IShoppingRepository
{
    private readonly AppDbContext _context;

    public ShoppingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MealPlan>> GetWeeklyPlansWithIngredientsAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default)
    {
        var weekEnd = weekStart.AddDays(7);

        return await _context.MealPlans
            .Where(m => m.FamilyGroupId == familyGroupId && m.Date >= weekStart && m.Date < weekEnd)
            .Include(m => m.MealPlanRecipes)
                .ThenInclude(mpr => mpr.Recipe)
                    .ThenInclude(r => r.Ingredients)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ShoppingListCheck>> GetChecksAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default)
    {
        return await _context.ShoppingListChecks
            .Where(s => s.FamilyGroupId == familyGroupId && s.WeekStartDate == weekStart)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task UpsertCheckAsync(int familyGroupId, DateOnly weekStart, string ingredientName, bool isChecked, CancellationToken ct = default)
    {
        var updated = await _context.ShoppingListChecks
            .Where(s =>
                s.FamilyGroupId == familyGroupId &&
                s.WeekStartDate == weekStart &&
                s.IngredientName == ingredientName)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsChecked, isChecked), ct);

        if (updated == 0)
        {
            _context.ShoppingListChecks.Add(new ShoppingListCheck
            {
                FamilyGroupId = familyGroupId,
                WeekStartDate = weekStart,
                IngredientName = ingredientName,
                IsChecked = isChecked
            });
            await _context.SaveChangesAsync(ct);
        }
    }
}
