using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using MenuCraft.Api.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.IdentityModel.Tokens;
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

    // Regression: the production JwtSecurityTokenHandler maps the inbound "role" claim to
    // ClaimTypes.Role (because MapInboundClaims defaults to true). GetUserRole / RequireAdmin
    // must work against the validated principal, not just the literal "role" claim type.
    [Fact]
    public void GetUserRole_WithJwtValidatedPrincipal_ReturnsRole()
    {
        var principal = ValidateJwtAsPrincipal("Admin");
        var context = CreateContextWithPrincipal(principal);

        context.GetUserRole().Should().Be("Admin");
    }

    [Fact]
    public void RequireAdmin_WithJwtValidatedAdminPrincipal_DoesNotThrow()
    {
        var principal = ValidateJwtAsPrincipal("Admin");
        var context = CreateContextWithPrincipal(principal);

        var act = () => context.RequireAdmin();

        act.Should().NotThrow();
    }

    private static FunctionContext CreateContextWithPrincipal(ClaimsPrincipal principal)
    {
        var items = new Dictionary<object, object?> { ["User"] = principal };
        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.Items).Returns(items);
        return mockContext.Object;
    }

    private static ClaimsPrincipal ValidateJwtAsPrincipal(string role)
    {
        const string secret = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!";
        const string issuer = "https://test.example.com";
        const string audience = "menucraft-api-test";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("role", role),
        };
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);
        var serialized = new JwtSecurityTokenHandler().WriteToken(token);

        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        return new JwtSecurityTokenHandler().ValidateToken(serialized, validation, out _);
    }
}
