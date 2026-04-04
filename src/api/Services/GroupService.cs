using MenuCraft.Api.Dtos.Groups;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Services;

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<GroupService> _logger;

    public GroupService(
        IGroupRepository groupRepository,
        UserManager<User> userManager,
        ILogger<GroupService> logger)
    {
        _groupRepository = groupRepository;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<GroupResponse> CreateGroupAsync(Guid userId, CreateGroupRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("ユーザーが見つかりません");

        if (user.FamilyGroupId.HasValue)
        {
            throw new InvalidOperationException("既にグループに所属しています");
        }

        var inviteCode = InviteCodeGenerator.Generate();

        var group = new FamilyGroup
        {
            Name = request.Name,
            InviteCode = inviteCode
        };

        var created = await _groupRepository.CreateAsync(group, ct);

        user.FamilyGroupId = created.Id;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Group created: {GroupName} by user {UserId}", request.Name, userId);

        return new GroupResponse(created.Id, created.Name, created.InviteCode);
    }

    public async Task<GroupResponse> JoinGroupAsync(Guid userId, JoinGroupRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("ユーザーが見つかりません");

        if (user.FamilyGroupId.HasValue)
        {
            throw new InvalidOperationException("既にグループに所属しています");
        }

        var group = await _groupRepository.FindByInviteCodeAsync(request.InviteCode, ct)
            ?? throw new KeyNotFoundException("招待コードが無効です");

        user.FamilyGroupId = group.Id;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {UserId} joined group {GroupId}", userId, group.Id);

        return new GroupResponse(group.Id, group.Name, group.InviteCode);
    }

    public async Task<IReadOnlyList<MemberResponse>> GetMembersAsync(int familyGroupId, CancellationToken ct = default)
    {
        var members = await _groupRepository.GetMembersAsync(familyGroupId, ct);
        return members
            .Select(m => new MemberResponse(m.Id, m.Email ?? string.Empty))
            .ToList();
    }


}
