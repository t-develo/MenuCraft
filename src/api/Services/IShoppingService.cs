using MenuCraft.Api.Dtos.Shopping;

namespace MenuCraft.Api.Services;

public interface IShoppingService
{
    Task<ShoppingListResponse> GetShoppingListAsync(int familyGroupId, DateOnly weekStart, CancellationToken ct = default);
    Task UpdateCheckAsync(int familyGroupId, UpdateCheckRequest request, CancellationToken ct = default);
}
