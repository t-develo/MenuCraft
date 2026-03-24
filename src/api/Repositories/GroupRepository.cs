using MenuCraft.Api.Data;
using MenuCraft.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuCraft.Api.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly AppDbContext _context;

    public GroupRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FamilyGroup?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.FamilyGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public async Task<FamilyGroup?> FindByInviteCodeAsync(string inviteCode, CancellationToken ct = default)
    {
        return await _context.FamilyGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.InviteCode == inviteCode, ct);
    }

    public async Task<FamilyGroup> CreateAsync(FamilyGroup group, CancellationToken ct = default)
    {
        _context.FamilyGroups.Add(group);
        await _context.SaveChangesAsync(ct);
        return group;
    }

    public async Task<IReadOnlyList<User>> GetMembersAsync(int groupId, CancellationToken ct = default)
    {
        return await _context.Users
            .Where(u => u.FamilyGroupId == groupId)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
