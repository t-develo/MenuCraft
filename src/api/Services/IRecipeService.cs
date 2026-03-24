using MenuCraft.Api.Dtos.Recipes;

namespace MenuCraft.Api.Services;

public interface IRecipeService
{
    Task<RecipeResponse?> GetRecipeAsync(int id, int familyGroupId, CancellationToken ct = default);
    Task<IReadOnlyList<RecipeResponse>> GetRecipesAsync(int familyGroupId, CancellationToken ct = default);
    Task<RecipeResponse> CreateRecipeAsync(int familyGroupId, CreateRecipeRequest request, CancellationToken ct = default);
    Task<RecipeResponse?> UpdateRecipeAsync(int id, int familyGroupId, UpdateRecipeRequest request, CancellationToken ct = default);
    Task<bool> DeleteRecipeAsync(int id, int familyGroupId, CancellationToken ct = default);
}
