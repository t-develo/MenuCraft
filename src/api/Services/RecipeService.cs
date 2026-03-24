using MenuCraft.Api.Dtos.Recipes;
using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;
using MenuCraft.Api.Repositories;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Services;

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _repo;
    private readonly ILogger<RecipeService> _logger;

    public RecipeService(IRecipeRepository repo, ILogger<RecipeService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<RecipeResponse?> GetRecipeAsync(int id, int familyGroupId, CancellationToken ct = default)
    {
        var recipe = await _repo.FindByIdAsync(id, ct);
        if (recipe is null || recipe.FamilyGroupId != familyGroupId)
        {
            return null;
        }

        return MapToResponse(recipe);
    }

    public async Task<IReadOnlyList<RecipeResponse>> GetRecipesAsync(int familyGroupId, CancellationToken ct = default)
    {
        var recipes = await _repo.GetByGroupIdAsync(familyGroupId, ct);
        return recipes.Select(MapToResponse).ToList();
    }

    public async Task<RecipeResponse> CreateRecipeAsync(int familyGroupId, CreateRecipeRequest request, CancellationToken ct = default)
    {
        var recipe = new Recipe
        {
            FamilyGroupId = familyGroupId,
            Title = request.Title.Trim(),
            Url = request.Url?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            Description = request.Description?.Trim(),
            SourceType = DetermineSourceType(request.Url),
            Tags = request.Tags
                .Select(t => new RecipeTag { Name = t.Trim() })
                .ToList(),
            Ingredients = request.Ingredients
                .Select(i => new RecipeIngredient
                {
                    Name = i.Name.Trim(),
                    Quantity = i.Quantity?.Trim(),
                    Unit = i.Unit?.Trim()
                })
                .ToList()
        };

        var created = await _repo.CreateAsync(recipe, ct);
        _logger.LogInformation("Recipe created: {RecipeId} in group {GroupId}", created.Id, familyGroupId);

        return MapToResponse(created);
    }

    public async Task<RecipeResponse?> UpdateRecipeAsync(int id, int familyGroupId, UpdateRecipeRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.FindByIdAsync(id, ct);
        if (existing is null || existing.FamilyGroupId != familyGroupId)
        {
            return null;
        }

        var updated = new Recipe
        {
            Id = id,
            FamilyGroupId = familyGroupId,
            Title = request.Title.Trim(),
            Url = request.Url?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            Description = request.Description?.Trim(),
            SourceType = DetermineSourceType(request.Url),
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            Tags = request.Tags
                .Select(t => new RecipeTag { Name = t.Trim() })
                .ToList(),
            Ingredients = request.Ingredients
                .Select(i => new RecipeIngredient
                {
                    Name = i.Name.Trim(),
                    Quantity = i.Quantity?.Trim(),
                    Unit = i.Unit?.Trim()
                })
                .ToList()
        };

        var result = await _repo.UpdateAsync(updated, ct);
        _logger.LogInformation("Recipe updated: {RecipeId}", id);

        return MapToResponse(result);
    }

    public async Task<bool> DeleteRecipeAsync(int id, int familyGroupId, CancellationToken ct = default)
    {
        var recipe = await _repo.FindByIdAsync(id, ct);
        if (recipe is null || recipe.FamilyGroupId != familyGroupId)
        {
            return false;
        }

        await _repo.DeleteAsync(id, ct);
        _logger.LogInformation("Recipe deleted: {RecipeId}", id);
        return true;
    }

    private static RecipeResponse MapToResponse(Recipe recipe) => new(
        recipe.Id,
        recipe.Title,
        recipe.Url,
        recipe.ImageUrl,
        recipe.Description,
        recipe.SourceType.ToString(),
        recipe.Tags.Select(t => t.Name).ToList(),
        recipe.Ingredients.Select(i => new IngredientResponse(i.Name, i.Quantity, i.Unit)).ToList(),
        recipe.CreatedAt
    );

    private static SourceType DetermineSourceType(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return SourceType.Manual;
        }

        if (url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            return SourceType.YouTube;
        }

        if (url.Contains("instagram.com", StringComparison.OrdinalIgnoreCase))
        {
            return SourceType.Instagram;
        }

        return SourceType.Web;
    }
}
