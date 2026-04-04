#nullable enable

using FluentAssertions;
using MenuCraft.Api.Data;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MenuCraft.Api.Tests.Repositories;

public class GroupRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GroupRepository _sut;

    public GroupRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _sut = new GroupRepository(_context);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WhenGroupsExist_ReturnsAllGroupsWithMembers()
    {
        // Arrange
        var group1 = new FamilyGroup { Name = "家族A", InviteCode = "AAAA1111" };
        var group2 = new FamilyGroup { Name = "家族B", InviteCode = "BBBB2222" };
        _context.FamilyGroups.AddRange(group1, group2);
        await _context.SaveChangesAsync();

        var user1 = new User { Email = "u1@example.com", UserName = "u1@example.com", FamilyGroupId = group1.Id };
        var user2 = new User { Email = "u2@example.com", UserName = "u2@example.com", FamilyGroupId = group1.Id };
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        var resultGroup1 = result.First(g => g.Id == group1.Id);
        resultGroup1.Members.Should().HaveCount(2);
        var resultGroup2 = result.First(g => g.Id == group2.Id);
        resultGroup2.Members.Should().HaveCount(0);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoGroupsExist_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithChangedName_PersistsNewName()
    {
        // Arrange
        var group = new FamilyGroup { Name = "旧名前", InviteCode = "OLD12345" };
        _context.FamilyGroups.Add(group);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        group.Name = "新名前";

        // Act
        var updated = await _sut.UpdateAsync(group);

        // Assert
        updated.Name.Should().Be("新名前");
        var fromDb = await _context.FamilyGroups.FindAsync(group.Id);
        fromDb!.Name.Should().Be("新名前");
    }

    [Fact]
    public async Task UpdateAsync_WithChangedInviteCode_PersistsNewCode()
    {
        // Arrange
        var group = new FamilyGroup { Name = "家族", InviteCode = "OLDCODE1" };
        _context.FamilyGroups.Add(group);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        group.InviteCode = "NEWCODE1";

        // Act
        var updated = await _sut.UpdateAsync(group);

        // Assert
        updated.InviteCode.Should().Be("NEWCODE1");
        var fromDb = await _context.FamilyGroups.FindAsync(group.Id);
        fromDb!.InviteCode.Should().Be("NEWCODE1");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WithValidId_RemovesGroup()
    {
        // Arrange
        var group = new FamilyGroup { Name = "削除対象", InviteCode = "DEL12345" };
        _context.FamilyGroups.Add(group);
        await _context.SaveChangesAsync();
        var groupId = group.Id;

        // Act
        await _sut.DeleteAsync(groupId);

        // Assert
        var fromDb = await _context.FamilyGroups.FindAsync(groupId);
        fromDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.DeleteAsync(999));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
