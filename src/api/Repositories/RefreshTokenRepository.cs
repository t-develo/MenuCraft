#nullable enable

using MenuCraft.Api.Data;
using MenuCraft.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuCraft.Api.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token, ct);
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(ct);
        return refreshToken;
    }

    public async Task RevokeAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        refreshToken.IsRevoked = true;
        await _context.SaveChangesAsync(ct);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        await _context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true), ct);
    }

    public async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        await _context.RefreshTokens
            .Where(r => r.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync(ct);
    }
}
