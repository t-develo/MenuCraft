using MenuCraft.Api.Models;

namespace MenuCraft.Api.Repositories;

public interface IRecipeRepository
{
    Task<Recipe?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Recipe>> GetByGroupIdAsync(int groupId, CancellationToken ct = default);
    Task<Recipe> CreateAsync(Recipe recipe, CancellationToken ct = default);
    Task<Recipe> UpdateAsync(Recipe recipe, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
