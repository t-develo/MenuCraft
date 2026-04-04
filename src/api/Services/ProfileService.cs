#nullable enable

using MenuCraft.Api.Dtos.Auth;
using MenuCraft.Api.Dtos.Profile;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<User> _userManager;
    private readonly IGroupRepository _groupRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        UserManager<User> userManager,
        IGroupRepository groupRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        ILogger<ProfileService> logger)
    {
        _userManager = userManager;
        _groupRepository = groupRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<ProfileResponse> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"ユーザー (Id={userId}) が見つかりません");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        // ナビゲーションプロパティが未ロードの場合にリポジトリから取得
        FamilyGroup? group = user.FamilyGroup;
        if (group is null && user.FamilyGroupId.HasValue)
        {
            group = await _groupRepository.FindByIdAsync(user.FamilyGroupId.Value, ct);
        }

        return new ProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            role,
            user.FamilyGroupId,
            group?.Name,
            group?.InviteCode,
            user.CreatedAt);
    }

    public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"ユーザー (Id={userId}) が見つかりません");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"パスワード変更に失敗しました: {errors}");
        }

        _logger.LogInformation("User {UserId} changed password", userId);

        // 全デバイスのリフレッシュトークンを失効
        await _refreshTokenRepository.RevokeAllForUserAsync(userId, ct);

        // 新しいトークンペアを発行
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";
        var accessToken = _jwtTokenService.GenerateAccessToken(user, role);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        };
        await _refreshTokenRepository.CreateAsync(refreshToken, ct);

        return new AuthResponse(accessToken, refreshTokenValue, DateTime.UtcNow.AddHours(1));
    }

    public async Task<AuthResponse> LeaveGroupAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"ユーザー (Id={userId}) が見つかりません");

        if (user.FamilyGroupId is null)
        {
            throw new InvalidOperationException("グループに参加していません");
        }

        user.FamilyGroupId = null;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {UserId} left group", userId);

        // familyGroupId クレームなしの新しいアクセストークンを発行
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";
        var accessToken = _jwtTokenService.GenerateAccessToken(user, role);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        };
        await _refreshTokenRepository.CreateAsync(refreshToken, ct);

        return new AuthResponse(accessToken, refreshTokenValue, DateTime.UtcNow.AddHours(1));
    }
}
