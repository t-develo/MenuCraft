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
        var token = _sut.GenerateAccessToken(user);

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
        var token = _sut.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
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

    [Fact]
    public void ValidateRefreshToken_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var token = _sut.GenerateRefreshToken();

        // Act
        var result = _sut.ValidateRefreshToken(token);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-base64")]
    public void ValidateRefreshToken_WithInvalidToken_ReturnsFalse(string invalidToken)
    {
        // Act
        var result = _sut.ValidateRefreshToken(invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateRefreshToken_WithNull_ReturnsFalse()
    {
        // Act
        var result = _sut.ValidateRefreshToken(null!);

        // Assert
        result.Should().BeFalse();
    }
}
