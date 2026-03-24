using MenuCraft.Api.Models;

namespace MenuCraft.Api.Repositories;

public interface IGroupRepository
{
    Task<FamilyGroup?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<FamilyGroup?> FindByInviteCodeAsync(string inviteCode, CancellationToken ct = default);
    Task<FamilyGroup> CreateAsync(FamilyGroup group, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetMembersAsync(int groupId, CancellationToken ct = default);
}
