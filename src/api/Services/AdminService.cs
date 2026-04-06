#nullable enable

using MenuCraft.Api.Dtos.Admin;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Services;

public class AdminService : IAdminService
{
    private static readonly HashSet<string> ValidRoles = new(StringComparer.Ordinal) { "Admin", "User" };

    private readonly UserManager<User> _userManager;
    private readonly IGroupRepository _groupRepository;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        UserManager<User> userManager,
        IGroupRepository groupRepository,
        ILogger<AdminService> logger)
    {
        _userManager = userManager;
        _groupRepository = groupRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserListResponse>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = _userManager.Users.AsNoTracking().ToList();

        // Load all groups once for an efficient lookup (avoids calling Include on UserManager,
        // which is an Identity abstraction and should not be used with EF Core navigation loading).
        var groupMap = (await _groupRepository.GetAllAsync(ct))
            .ToDictionary(g => g.Id);

        var result = new List<UserListResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;

            FamilyGroup? group = null;
            if (user.FamilyGroupId.HasValue)
            {
                groupMap.TryGetValue(user.FamilyGroupId.Value, out group);
            }

            result.Add(new UserListResponse(
                user.Id,
                user.Email ?? string.Empty,
                role,
                user.FamilyGroupId,
                group?.Name,
                user.CreatedAt));
        }

        return result;
    }

    public async Task ChangeUserRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        if (!ValidRoles.Contains(role))
        {
            throw new ArgumentException($"無効なロールです: {role}。有効な値: Admin, User", nameof(role));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"ユーザー (Id={userId}) が見つかりません");

        // 最後のAdminの降格を防止
        if (role == "User")
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    throw new InvalidOperationException("最後のAdminを降格することはできません");
                }
            }
        }

        var existingRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, existingRoles);
        await _userManager.AddToRoleAsync(user, role);

        _logger.LogInformation("User {UserId} role changed to {Role}", userId, role);
    }

    public async Task<IReadOnlyList<GroupListResponse>> GetAllGroupsAsync(CancellationToken ct = default)
    {
        var groups = await _groupRepository.GetAllAsync(ct);
        return groups
            .Select(g => new GroupListResponse(
                g.Id,
                g.Name,
                g.InviteCode,
                g.Members.Count,
                g.CreatedAt))
            .ToList();
    }

    public async Task<GroupListResponse> UpdateGroupNameAsync(int groupId, string name, CancellationToken ct = default)
    {
        var group = await _groupRepository.FindByIdAsync(groupId, ct)
            ?? throw new KeyNotFoundException($"グループ (Id={groupId}) が見つかりません");

        group.Name = name;
        var updated = await _groupRepository.UpdateAsync(group, ct);
        var members = await _groupRepository.GetMembersAsync(updated.Id, ct);

        _logger.LogInformation("Group {GroupId} name updated to {Name}", groupId, name);

        return new GroupListResponse(updated.Id, updated.Name, updated.InviteCode, members.Count, updated.CreatedAt);
    }

    public async Task RemoveMemberFromGroupAsync(int groupId, Guid userId, CancellationToken ct = default)
    {
        _ = await _groupRepository.FindByIdAsync(groupId, ct)
            ?? throw new KeyNotFoundException($"グループ (Id={groupId}) が見つかりません");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"ユーザー (Id={userId}) が見つかりません");

        user.FamilyGroupId = null;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {UserId} removed from group {GroupId}", userId, groupId);
    }

    public async Task DeleteGroupAsync(int groupId, CancellationToken ct = default)
    {
        _ = await _groupRepository.FindByIdAsync(groupId, ct)
            ?? throw new KeyNotFoundException($"グループ (Id={groupId}) が見つかりません");

        await _groupRepository.DeleteAsync(groupId, ct);

        _logger.LogInformation("Group {GroupId} deleted", groupId);
    }

    public async Task<GroupListResponse> RegenerateInviteCodeAsync(int groupId, CancellationToken ct = default)
    {
        var group = await _groupRepository.FindByIdAsync(groupId, ct)
            ?? throw new KeyNotFoundException($"グループ (Id={groupId}) が見つかりません");

        group.InviteCode = InviteCodeGenerator.Generate();
        var updated = await _groupRepository.UpdateAsync(group, ct);
        var members = await _groupRepository.GetMembersAsync(updated.Id, ct);

        _logger.LogInformation("Group {GroupId} invite code regenerated", groupId);

        return new GroupListResponse(updated.Id, updated.Name, updated.InviteCode, members.Count, updated.CreatedAt);
    }
}
