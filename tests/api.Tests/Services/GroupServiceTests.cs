using FluentAssertions;
using MenuCraft.Api.Dtos.Groups;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuCraft.Api.Tests.Services;

public class GroupServiceTests
{
    private readonly Mock<IGroupRepository> _mockGroupRepo;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly GroupService _sut;

    public GroupServiceTests()
    {
        _mockGroupRepo = new Mock<IGroupRepository>();

        var store = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var logger = Mock.Of<ILogger<GroupService>>();
        _sut = new GroupService(_mockGroupRepo.Object, _mockUserManager.Object, logger);
    }

    [Fact]
    public async Task CreateGroupAsync_WithValidUser_CreatesGroupAndAssignsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", FamilyGroupId = null };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockGroupRepo.Setup(r => r.CreateAsync(It.IsAny<FamilyGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyGroup g, CancellationToken _) =>
            {
                return new FamilyGroup { Id = 1, Name = g.Name, InviteCode = g.InviteCode };
            });
        _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var request = new CreateGroupRequest("テスト家族");

        // Act
        var result = await _sut.CreateGroupAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("テスト家族");
        result.InviteCode.Should().NotBeNullOrWhiteSpace();
        result.InviteCode.Should().HaveLength(8);
    }

    [Fact]
    public async Task CreateGroupAsync_UserAlreadyInGroup_ThrowsInvalidOperation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, FamilyGroupId = 42 };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var request = new CreateGroupRequest("テスト");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateGroupAsync(userId, request));
    }

    [Fact]
    public async Task JoinGroupAsync_WithValidInviteCode_JoinsGroup()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, FamilyGroupId = null };
        var group = new FamilyGroup { Id = 1, Name = "既存グループ", InviteCode = "ABC12345" };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockGroupRepo.Setup(r => r.FindByInviteCodeAsync("ABC12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var request = new JoinGroupRequest("ABC12345");

        // Act
        var result = await _sut.JoinGroupAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("既存グループ");
    }

    [Fact]
    public async Task JoinGroupAsync_WithInvalidCode_ThrowsKeyNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, FamilyGroupId = null };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockGroupRepo.Setup(r => r.FindByInviteCodeAsync("INVALID", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyGroup?)null);

        var request = new JoinGroupRequest("INVALID");

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.JoinGroupAsync(userId, request));
    }

    [Fact]
    public async Task JoinGroupAsync_UserAlreadyInGroup_ThrowsInvalidOperation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, FamilyGroupId = 99 };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var request = new JoinGroupRequest("ABC12345");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.JoinGroupAsync(userId, request));
    }

    [Fact]
    public async Task GetMembersAsync_ReturnsMemberList()
    {
        // Arrange
        var members = new List<User>
        {
            new() { Id = Guid.NewGuid(), Email = "user1@example.com" },
            new() { Id = Guid.NewGuid(), Email = "user2@example.com" }
        };
        _mockGroupRepo.Setup(r => r.GetMembersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        // Act
        var result = await _sut.GetMembersAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result[0].Email.Should().Be("user1@example.com");
    }
}
