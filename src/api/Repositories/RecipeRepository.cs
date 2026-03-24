using MenuCraft.Api.Data;
using MenuCraft.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuCraft.Api.Repositories;

public class RecipeRepository : IRecipeRepository
{
    private readonly AppDbContext _context;

    public RecipeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Recipe?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Recipes
            .Include(r => r.Tags)
            .Include(r => r.Ingredients)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
    }

    public async Task<IReadOnlyList<Recipe>> GetByGroupIdAsync(int groupId, CancellationToken ct = default)
    {
        return await _context.Recipes
            .Where(r => r.FamilyGroupId == groupId && !r.IsDeleted)
            .Include(r => r.Tags)
            .Include(r => r.Ingredients)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Recipe> CreateAsync(Recipe recipe, CancellationToken ct = default)
    {
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync(ct);
        return recipe;
    }

    public async Task<Recipe> UpdateAsync(Recipe recipe, CancellationToken ct = default)
    {
        var existing = await _context.Recipes
            .Include(r => r.Tags)
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == recipe.Id, ct)
            ?? throw new KeyNotFoundException($"Recipe {recipe.Id} not found");

        _context.Entry(existing).CurrentValues.SetValues(recipe);

        _context.RecipeTags.RemoveRange(existing.Tags);
        foreach (var tag in recipe.Tags)
        {
            existing.Tags.Add(tag);
        }

        _context.RecipeIngredients.RemoveRange(existing.Ingredients);
        foreach (var ingredient in recipe.Ingredients)
        {
            existing.Ingredients.Add(ingredient);
        }

        await _context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var recipe = await _context.Recipes.FindAsync(new object[] { id }, ct);
        if (recipe is not null)
        {
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync(ct);
        }
    }
}
