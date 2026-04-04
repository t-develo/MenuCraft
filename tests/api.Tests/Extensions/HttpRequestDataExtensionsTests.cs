using System.Security.Claims;
using FluentAssertions;
using MenuCraft.Api.Extensions;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace MenuCraft.Api.Tests.Extensions;

public class HttpRequestDataExtensionsTests
{
    private static FunctionContext CreateContextWithClaims(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var items = new Dictionary<object, object?> { ["User"] = principal };
        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.Items).Returns(items);
        return mockContext.Object;
    }

    private static FunctionContext CreateContextWithRole(string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("role", role)
        };
        return CreateContextWithClaims(claims);
    }

    private static FunctionContext CreateContextWithoutRole()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        return CreateContextWithClaims(claims);
    }

    [Fact]
    public void GetUserRole_WithAdminRoleClaim_ReturnsAdmin()
    {
        // Arrange
        var context = CreateContextWithRole("Admin");

        // Act
        var role = context.GetUserRole();

        // Assert
        role.Should().Be("Admin");
    }

    [Fact]
    public void GetUserRole_WithUserRoleClaim_ReturnsUser()
    {
        // Arrange
        var context = CreateContextWithRole("User");

        // Act
        var role = context.GetUserRole();

        // Assert
        role.Should().Be("User");
    }

    [Fact]
    public void GetUserRole_WithoutRoleClaim_ReturnsNull()
    {
        // Arrange
        var context = CreateContextWithoutRole();

        // Act
        var role = context.GetUserRole();

        // Assert
        role.Should().BeNull();
    }

    [Fact]
    public void RequireAdmin_WithAdminRole_DoesNotThrow()
    {
        // Arrange
        var context = CreateContextWithRole("Admin");

        // Act
        var act = () => context.RequireAdmin();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireAdmin_WithUserRole_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var context = CreateContextWithRole("User");

        // Act
        var act = () => context.RequireAdmin();

        // Assert
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void RequireAdmin_WithoutRoleClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var context = CreateContextWithoutRole();

        // Act
        var act = () => context.RequireAdmin();

        // Assert
        act.Should().Throw<UnauthorizedAccessException>();
    }
}
