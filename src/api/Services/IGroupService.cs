using MenuCraft.Api.Dtos.Groups;

namespace MenuCraft.Api.Services;

public interface IGroupService
{
    Task<GroupResponse> CreateGroupAsync(Guid userId, CreateGroupRequest request, CancellationToken ct = default);
    Task<GroupResponse> JoinGroupAsync(Guid userId, JoinGroupRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<MemberResponse>> GetMembersAsync(int familyGroupId, CancellationToken ct = default);
}
