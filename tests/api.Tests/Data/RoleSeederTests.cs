using FluentAssertions;
using MenuCraft.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuCraft.Api.Tests.Data;

public class RoleSeederTests
{
    private static Mock<RoleManager<IdentityRole<Guid>>> CreateMockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole<Guid>>>();
        return new Mock<RoleManager<IdentityRole<Guid>>>(
            store.Object, null!, null!, null!, null!);
    }

    [Fact]
    public async Task SeedAsync_WhenNoRolesExist_CreatesAdminAndUserRoles()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager
            .Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        mockRoleManager
            .Setup(m => m.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await RoleSeeder.SeedAsync(mockRoleManager.Object);

        // Assert
        mockRoleManager.Verify(
            m => m.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == "Admin")),
            Times.Once);
        mockRoleManager.Verify(
            m => m.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == "User")),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenRolesAlreadyExist_DoesNotCreateDuplicates()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager
            .Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await RoleSeeder.SeedAsync(mockRoleManager.Object);

        // Assert
        mockRoleManager.Verify(
            m => m.CreateAsync(It.IsAny<IdentityRole<Guid>>()),
            Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminExistsButUserDoesNot_CreatesOnlyUserRole()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager
            .Setup(m => m.RoleExistsAsync("Admin"))
            .ReturnsAsync(true);
        mockRoleManager
            .Setup(m => m.RoleExistsAsync("User"))
            .ReturnsAsync(false);
        mockRoleManager
            .Setup(m => m.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await RoleSeeder.SeedAsync(mockRoleManager.Object);

        // Assert
        mockRoleManager.Verify(
            m => m.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == "Admin")),
            Times.Never);
        mockRoleManager.Verify(
            m => m.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == "User")),
            Times.Once);
    }
}
