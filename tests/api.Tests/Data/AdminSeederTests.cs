using FluentAssertions;
using MenuCraft.Api.Data;
using MenuCraft.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuCraft.Api.Tests.Data;

public class AdminSeederTests
{
    private const string Email = "admin@menucraft.local";
    private const string Password = "S33d-Passw0rd!";

    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly ILogger _logger = Mock.Of<ILogger>();

    public AdminSeederTests()
    {
        var store = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Theory]
    [InlineData(null, Password)]
    [InlineData(Email, null)]
    [InlineData("", Password)]
    [InlineData(Email, "   ")]
    [InlineData(null, null)]
    public async Task SeedAsync_WhenConfigurationIncomplete_DoesNothing(string? email, string? password)
    {
        await AdminSeeder.SeedAsync(_mockUserManager.Object, email, password, _logger);

        _mockUserManager.Verify(
            m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _mockUserManager.Verify(
            m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminDoesNotExist_CreatesUserWithAdminRole()
    {
        _mockUserManager
            .Setup(m => m.FindByEmailAsync(Email))
            .ReturnsAsync((User?)null);
        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        await AdminSeeder.SeedAsync(_mockUserManager.Object, Email, Password, _logger);

        _mockUserManager.Verify(
            m => m.CreateAsync(
                It.Is<User>(u => u.Email == Email && u.UserName == Email && u.EmailConfirmed),
                Password),
            Times.Once);
        _mockUserManager.Verify(
            m => m.AddToRoleAsync(It.IsAny<User>(), "Admin"), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_TrimsTheConfiguredEmail()
    {
        _mockUserManager
            .Setup(m => m.FindByEmailAsync(Email))
            .ReturnsAsync((User?)null);
        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        await AdminSeeder.SeedAsync(_mockUserManager.Object, $"  {Email}  ", Password, _logger);

        _mockUserManager.Verify(m => m.FindByEmailAsync(Email), Times.Once);
        _mockUserManager.Verify(
            m => m.CreateAsync(It.Is<User>(u => u.Email == Email), Password), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminAlreadyExistsWithRole_DoesNotRecreateOrChangePassword()
    {
        var existing = new User { Email = Email, UserName = Email };
        _mockUserManager
            .Setup(m => m.FindByEmailAsync(Email))
            .ReturnsAsync(existing);
        _mockUserManager
            .Setup(m => m.IsInRoleAsync(existing, "Admin"))
            .ReturnsAsync(true);

        await AdminSeeder.SeedAsync(_mockUserManager.Object, Email, Password, _logger);

        _mockUserManager.Verify(
            m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _mockUserManager.Verify(
            m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenExistingUserLacksAdminRole_GrantsIt()
    {
        var existing = new User { Email = Email, UserName = Email };
        _mockUserManager
            .Setup(m => m.FindByEmailAsync(Email))
            .ReturnsAsync(existing);
        _mockUserManager
            .Setup(m => m.IsInRoleAsync(existing, "Admin"))
            .ReturnsAsync(false);
        _mockUserManager
            .Setup(m => m.AddToRoleAsync(existing, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        await AdminSeeder.SeedAsync(_mockUserManager.Object, Email, Password, _logger);

        _mockUserManager.Verify(m => m.AddToRoleAsync(existing, "Admin"), Times.Once);
        _mockUserManager.Verify(
            m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenCreateFails_ThrowsWithTheIdentityError()
    {
        _mockUserManager
            .Setup(m => m.FindByEmailAsync(Email))
            .ReturnsAsync((User?)null);
        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "パスワードが短すぎます",
            }));

        var act = () => AdminSeeder.SeedAsync(_mockUserManager.Object, Email, Password, _logger);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*パスワードが短すぎます*");
    }

    [Fact]
    public async Task SeedAsync_WhenRoleAssignmentFails_Throws()
    {
        _mockUserManager
            .Setup(m => m.FindByEmailAsync(Email))
            .ReturnsAsync((User?)null);
        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Admin"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "RoleNotFound",
                Description = "ロールが見つかりません",
            }));

        var act = () => AdminSeeder.SeedAsync(_mockUserManager.Object, Email, Password, _logger);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ロールが見つかりません*");
    }
}
