using MenuCraft.Api.Data;
using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuCraft.Api.Repositories;

public class MealPlanRepository : IMealPlanRepository
{
    private readonly AppDbContext _context;

    public MealPlanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MealPlan>> GetWeeklyPlansAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default)
    {
        var weekEnd = weekStart.AddDays(7);

        return await _context.MealPlans
            .Where(m => m.FamilyGroupId == familyGroupId && m.Date >= weekStart && m.Date < weekEnd)
            .Include(m => m.MealPlanRecipes)
                .ThenInclude(mpr => mpr.Recipe)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<MealPlan?> GetByDateAndTypeAsync(int familyGroupId, DateOnly date, MealType mealType, CancellationToken ct = default)
    {
        return await _context.MealPlans
            .Where(m => m.FamilyGroupId == familyGroupId && m.Date == date && m.MealType == mealType)
            .Include(m => m.MealPlanRecipes)
                .ThenInclude(mpr => mpr.Recipe)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    public async Task<MealPlan> CreateOrUpdateAsync(int familyGroupId, DateOnly date, MealType mealType, IReadOnlyList<int> recipeIds, CancellationToken ct = default)
    {
        var existing = await _context.MealPlans
            .Where(m => m.FamilyGroupId == familyGroupId && m.Date == date && m.MealType == mealType)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            // Full replacement: delete all existing recipe associations, then insert new ones
            await _context.MealPlanRecipes
                .Where(mpr => mpr.MealPlanId == existing.Id)
                .ExecuteDeleteAsync(ct);

            var newRecipes = recipeIds
                .Select(id => new MealPlanRecipe { MealPlanId = existing.Id, RecipeId = id })
                .ToList();
            _context.MealPlanRecipes.AddRange(newRecipes);
            _context.Entry(existing).Property(m => m.UpdatedAt).CurrentValue = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
        else
        {
            var mealPlan = new MealPlan
            {
                FamilyGroupId = familyGroupId,
                Date = date,
                MealType = mealType,
                MealPlanRecipes = recipeIds
                    .Select(id => new MealPlanRecipe { RecipeId = id })
                    .ToList()
            };
            _context.MealPlans.Add(mealPlan);
            await _context.SaveChangesAsync(ct);
        }

        return await _context.MealPlans
            .Where(m => m.FamilyGroupId == familyGroupId && m.Date == date && m.MealType == mealType)
            .Include(m => m.MealPlanRecipes)
                .ThenInclude(mpr => mpr.Recipe)
            .AsNoTracking()
            .FirstAsync(ct);
    }

    public async Task ClearWeekAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default)
    {
        var weekEnd = weekStart.AddDays(7);

        var plans = await _context.MealPlans
            .Where(m => m.FamilyGroupId == familyGroupId && m.Date >= weekStart && m.Date < weekEnd)
            .ToListAsync(ct);

        _context.MealPlans.RemoveRange(plans);
        await _context.SaveChangesAsync(ct);
    }
}
