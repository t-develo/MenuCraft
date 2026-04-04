#nullable enable

using MenuCraft.Api.Dtos.Admin;

namespace MenuCraft.Api.Services;

public interface IAdminService
{
    Task<IReadOnlyList<UserListResponse>> GetAllUsersAsync(CancellationToken ct = default);
    Task ChangeUserRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task<IReadOnlyList<GroupListResponse>> GetAllGroupsAsync(CancellationToken ct = default);
    Task<GroupListResponse> UpdateGroupNameAsync(int groupId, string name, CancellationToken ct = default);
    Task RemoveMemberFromGroupAsync(int groupId, Guid userId, CancellationToken ct = default);
    Task DeleteGroupAsync(int groupId, CancellationToken ct = default);
    Task<GroupListResponse> RegenerateInviteCodeAsync(int groupId, CancellationToken ct = default);
}
