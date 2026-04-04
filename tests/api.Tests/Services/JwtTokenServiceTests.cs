using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using MenuCraft.Api.Models;
using MenuCraft.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuCraft.Api.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!",
                ["Jwt:Issuer"] = "https://test.example.com",
                ["Jwt:Audience"] = "menucraft-api-test",
                ["Jwt:AccessTokenExpirationMinutes"] = "60"
            })
            .Build();

        var logger = Mock.Of<ILogger<JwtTokenService>>();
        _sut = new JwtTokenService(configuration, logger);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtString()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FamilyGroupId = 42
        };

        // Act
        var token = _sut.GenerateAccessToken(user, "User");

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3, "JWT should have 3 parts");
    }

    [Fact]
    public void GenerateAccessToken_WithoutFamilyGroup_ReturnsValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FamilyGroupId = null
        };

        // Act
        var token = _sut.GenerateAccessToken(user, "User");

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    public void GenerateAccessToken_WithRole_IncludesRoleClaimInToken(string role)
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FamilyGroupId = null
        };

        // Act
        var token = _sut.GenerateAccessToken(user, role);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        var roleClaim = parsed.Claims.FirstOrDefault(c => c.Type == "role");
        roleClaim.Should().NotBeNull("role claim should be present in JWT");
        roleClaim!.Value.Should().Be(role);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var token = _sut.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        var action = () => Convert.FromBase64String(token);
        action.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentTokensEachTime()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }
}
