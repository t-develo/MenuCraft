#nullable enable

using MenuCraft.Api.Models;

namespace MenuCraft.Api.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken ct = default);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken ct = default);
    Task RevokeAsync(RefreshToken refreshToken, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
    Task DeleteExpiredAsync(CancellationToken ct = default);
}
