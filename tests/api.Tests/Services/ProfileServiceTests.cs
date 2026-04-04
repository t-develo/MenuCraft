#nullable enable

using FluentAssertions;
using MenuCraft.Api.Dtos.Profile;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuCraft.Api.Tests.Services;

public class ProfileServiceTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IGroupRepository> _mockGroupRepo;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly ProfileService _sut;

    public ProfileServiceTests()
    {
        var store = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockGroupRepo = new Mock<IGroupRepository>();
        _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();

        var logger = Mock.Of<ILogger<ProfileService>>();
        _sut = new ProfileService(
            _mockUserManager.Object,
            _mockGroupRepo.Object,
            _mockRefreshTokenRepo.Object,
            _mockJwtTokenService.Object,
            logger);
    }

    // ── GetProfileAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_WithGroupMember_ReturnsProfileWithGroupInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = 1;
        var group = new FamilyGroup { Id = groupId, Name = "テスト家族", InviteCode = "ABCD1234" };
        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            FamilyGroupId = groupId,
            FamilyGroup = group,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be("user@example.com");
        result.Role.Should().Be("User");
        result.FamilyGroupId.Should().Be(groupId);
        result.GroupName.Should().Be("テスト家族");
        result.InviteCode.Should().Be("ABCD1234");
    }

    [Fact]
    public async Task GetProfileAsync_WithNoGroup_ReturnsProfileWithNullGroupInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            FamilyGroupId = null,
            FamilyGroup = null,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.FamilyGroupId.Should().BeNull();
        result.GroupName.Should().BeNull();
        result.InviteCode.Should().BeNull();
        result.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task GetProfileAsync_WithNoRole_ReturnsEmptyRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@example.com" };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.Role.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetProfileAsync(userId));
    }

    // ── ChangePasswordAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_WithValidPasswords_RevokesAllTokensAndReturnsNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@example.com" };
        var request = new ChangePasswordRequest("OldPass123!", "NewPass456!");
        const string newAccessToken = "new-access-token";
        const string newRefreshToken = "new-refresh-token";

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });
        _mockJwtTokenService.Setup(j => j.GenerateAccessToken(user, "User"))
            .Returns(newAccessToken);
        _mockJwtTokenService.Setup(j => j.GenerateRefreshToken())
            .Returns(newRefreshToken);

        // Act
        var result = await _sut.ChangePasswordAsync(userId, request);

        // Assert
        result.AccessToken.Should().Be(newAccessToken);
        result.RefreshToken.Should().Be(newRefreshToken);

        // パスワード変更後に全リフレッシュトークンを失効
        _mockRefreshTokenRepo.Verify(
            r => r.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);

        // 新しいリフレッシュトークンをDB保存
        _mockRefreshTokenRepo.Verify(
            r => r.CreateAsync(
                It.Is<RefreshToken>(t => t.UserId == userId && t.Token == newRefreshToken),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@example.com" };
        var request = new ChangePasswordRequest("WrongPass!", "NewPass456!");

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "現在のパスワードが正しくありません" }));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ChangePasswordAsync(userId, request));

        // 失敗時はリフレッシュトークンを失効させない
        _mockRefreshTokenRepo.Verify(
            r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        var request = new ChangePasswordRequest("OldPass123!", "NewPass456!");

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.ChangePasswordAsync(userId, request));
    }

    // ── LeaveGroupAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task LeaveGroupAsync_WithGroupMember_ClearsGroupAndReturnsNewToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@example.com", FamilyGroupId = 1 };
        const string newAccessToken = "new-access-token-no-group";
        const string newRefreshToken = "new-refresh-token-no-group";

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "User" });
        _mockJwtTokenService.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), "User"))
            .Returns(newAccessToken);
        _mockJwtTokenService.Setup(j => j.GenerateRefreshToken())
            .Returns(newRefreshToken);

        // Act
        var result = await _sut.LeaveGroupAsync(userId);

        // Assert
        result.AccessToken.Should().Be(newAccessToken);
        result.RefreshToken.Should().Be(newRefreshToken);

        // FamilyGroupId が null に設定される
        user.FamilyGroupId.Should().BeNull();
        _mockUserManager.Verify(m => m.UpdateAsync(user), Times.Once);

        // 新しいリフレッシュトークンをDB保存
        _mockRefreshTokenRepo.Verify(
            r => r.CreateAsync(
                It.Is<RefreshToken>(t => t.UserId == userId && t.Token == newRefreshToken),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LeaveGroupAsync_WithNoGroup_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@example.com", FamilyGroupId = null };

        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LeaveGroupAsync(userId));
    }

    [Fact]
    public async Task LeaveGroupAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.LeaveGroupAsync(userId));
    }
}
