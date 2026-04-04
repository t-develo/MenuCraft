#nullable enable

using FluentAssertions;
using MenuCraft.Api.Dtos.Admin;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Services;
using MenuCraft.Api.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuCraft.Api.Tests.Services;

public class AdminServiceTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IGroupRepository> _mockGroupRepo;
    private readonly AdminService _sut;

    public AdminServiceTests()
    {
        var store = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockGroupRepo = new Mock<IGroupRepository>();

        var logger = Mock.Of<ILogger<AdminService>>();
        _sut = new AdminService(_mockUserManager.Object, _mockGroupRepo.Object, logger);
    }

    // ── GetAllUsersAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllUsersWithRoleAndGroup()
    {
        // Arrange
        var groupId = 1;
        var group = new FamilyGroup { Id = groupId, Name = "テスト家族", InviteCode = "ABC12345" };
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Email = "admin@example.com", FamilyGroupId = groupId, FamilyGroup = group },
            new() { Id = Guid.NewGuid(), Email = "user@example.com", FamilyGroupId = null, FamilyGroup = null }
        };

        _mockUserManager.Setup(m => m.Users)
            .Returns(users.AsQueryable());
        _mockUserManager.Setup(m => m.GetRolesAsync(users[0]))
            .ReturnsAsync(new List<string> { "Admin" });
        _mockUserManager.Setup(m => m.GetRolesAsync(users[1]))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _sut.GetAllUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Email.Should().Be("admin@example.com");
        result[0].Role.Should().Be("Admin");
        result[0].FamilyGroupId.Should().Be(groupId);
        result[0].GroupName.Should().Be("テスト家族");
        result[1].Email.Should().Be("user@example.com");
        result[1].Role.Should().Be("User");
        result[1].FamilyGroupId.Should().BeNull();
        result[1].GroupName.Should().BeNull();
    }

    [Fact]
    public async Task GetAllUsersAsync_UserWithNoRole_ReturnsEmptyRole()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "norole@example.com" };
        _mockUserManager.Setup(m => m.Users)
            .Returns(new List<User> { user }.AsQueryable());
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.GetAllUsersAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Role.Should().Be(string.Empty);
    }

    // ── ChangeUserRoleAsync ───────────────────────────────────────────────

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    public async Task ChangeUserRoleAsync_WithValidRole_ChangesRole(string newRole)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });
        _mockUserManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.AddToRoleAsync(user, newRole))
            .ReturnsAsync(IdentityResult.Success);

        // If promoting to Admin, setup admin count check (2 admins)
        var adminUsers = new List<User> { new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() } };
        _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync(adminUsers);

        // Act
        await _sut.ChangeUserRoleAsync(userId, newRole);

        // Assert
        _mockUserManager.Verify(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        _mockUserManager.Verify(m => m.AddToRoleAsync(user, newRole), Times.Once);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WithInvalidUserId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.ChangeUserRoleAsync(userId, "User"));
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WithInvalidRole_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ChangeUserRoleAsync(userId, "SuperAdmin"));
    }

    [Fact]
    public async Task ChangeUserRoleAsync_DemotingLastAdmin_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });
        // Only one admin exists (the user being demoted)
        _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync(new List<User> { user });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ChangeUserRoleAsync(userId, "User"));
    }

    // ── GetAllGroupsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllGroupsAsync_ReturnsAllGroupsWithMemberCount()
    {
        // Arrange
        var groups = new List<FamilyGroup>
        {
            new()
            {
                Id = 1, Name = "家族A", InviteCode = "AAAA1111",
                Members = new List<User> { new(), new() }
            },
            new()
            {
                Id = 2, Name = "家族B", InviteCode = "BBBB2222",
                Members = new List<User>()
            }
        };

        _mockGroupRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(groups);

        // Act
        var result = await _sut.GetAllGroupsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("家族A");
        result[0].InviteCode.Should().Be("AAAA1111");
        result[0].MemberCount.Should().Be(2);
        result[1].MemberCount.Should().Be(0);
    }

    // ── UpdateGroupNameAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateGroupNameAsync_WithValidId_UpdatesNameAndReturnsResponse()
    {
        // Arrange
        var group = new FamilyGroup { Id = 1, Name = "旧グループ名", InviteCode = "ABCD1234" };
        _mockGroupRepo.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        FamilyGroup? updatedGroup = null;
        _mockGroupRepo.Setup(r => r.UpdateAsync(It.IsAny<FamilyGroup>(), It.IsAny<CancellationToken>()))
            .Callback<FamilyGroup, CancellationToken>((g, _) => updatedGroup = g)
            .ReturnsAsync((FamilyGroup g, CancellationToken _) => g);
        _mockGroupRepo.Setup(r => r.GetMembersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _sut.UpdateGroupNameAsync(1, "新グループ名");

        // Assert
        updatedGroup.Should().NotBeNull();
        updatedGroup!.Name.Should().Be("新グループ名");
        result.Name.Should().Be("新グループ名");
    }

    [Fact]
    public async Task UpdateGroupNameAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockGroupRepo.Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyGroup?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UpdateGroupNameAsync(999, "新名前"));
    }

    // ── RemoveMemberFromGroupAsync ────────────────────────────────────────

    [Fact]
    public async Task RemoveMemberFromGroupAsync_WithValidIds_SetsUserGroupToNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, FamilyGroupId = 1 };

        _mockGroupRepo.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FamilyGroup { Id = 1, Name = "家族", InviteCode = "TEST1234" });
        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _sut.RemoveMemberFromGroupAsync(1, userId);

        // Assert
        user.FamilyGroupId.Should().BeNull();
        _mockUserManager.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task RemoveMemberFromGroupAsync_WithInvalidGroupId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockGroupRepo.Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyGroup?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.RemoveMemberFromGroupAsync(999, Guid.NewGuid()));
    }

    [Fact]
    public async Task RemoveMemberFromGroupAsync_WithInvalidUserId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockGroupRepo.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FamilyGroup { Id = 1, Name = "家族", InviteCode = "TEST1234" });
        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.RemoveMemberFromGroupAsync(1, userId));
    }

    // ── DeleteGroupAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteGroupAsync_WithValidId_DeletesGroup()
    {
        // Arrange
        _mockGroupRepo.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FamilyGroup { Id = 1, Name = "家族", InviteCode = "TEST1234" });
        _mockGroupRepo.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteGroupAsync(1);

        // Assert
        _mockGroupRepo.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockGroupRepo.Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyGroup?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.DeleteGroupAsync(999));
    }

    // ── RegenerateInviteCodeAsync ─────────────────────────────────────────

    [Fact]
    public async Task RegenerateInviteCodeAsync_WithValidId_UpdatesInviteCode()
    {
        // Arrange
        var group = new FamilyGroup { Id = 1, Name = "家族", InviteCode = "OLDCODE1" };
        _mockGroupRepo.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        FamilyGroup? updatedGroup = null;
        _mockGroupRepo.Setup(r => r.UpdateAsync(It.IsAny<FamilyGroup>(), It.IsAny<CancellationToken>()))
            .Callback<FamilyGroup, CancellationToken>((g, _) => updatedGroup = g)
            .ReturnsAsync((FamilyGroup g, CancellationToken _) => g);
        _mockGroupRepo.Setup(r => r.GetMembersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _sut.RegenerateInviteCodeAsync(1);

        // Assert
        updatedGroup.Should().NotBeNull();
        updatedGroup!.InviteCode.Should().NotBe("OLDCODE1");
        updatedGroup.InviteCode.Should().HaveLength(8);
        result.InviteCode.Should().NotBe("OLDCODE1");
    }

    [Fact]
    public async Task RegenerateInviteCodeAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockGroupRepo.Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyGroup?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.RegenerateInviteCodeAsync(999));
    }
}
