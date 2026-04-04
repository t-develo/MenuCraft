#nullable enable

using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Azure.Core.Serialization;
using FluentAssertions;
using MenuCraft.Api.Dtos.Auth;
using MenuCraft.Api.Dtos.Profile;
using MenuCraft.Api.Functions;
using MenuCraft.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MenuCraft.Api.Tests.Functions;

public class ProfileFunctionTests
{
    private readonly Mock<IProfileService> _mockProfileService;
    private readonly ProfileFunction _sut;
    private readonly ServiceProvider _serviceProvider;

    public ProfileFunctionTests()
    {
        _mockProfileService = new Mock<IProfileService>();
        var logger = Mock.Of<ILogger<ProfileFunction>>();
        _sut = new ProfileFunction(_mockProfileService.Object, logger);

        _serviceProvider = new ServiceCollection()
            .Configure<WorkerOptions>(o => o.Serializer = new JsonObjectSerializer(
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
            .BuildServiceProvider();
    }

    private FunctionContext CreateAuthenticatedContext(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("role", "User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var items = new Dictionary<object, object?> { ["User"] = principal };
        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.Items).Returns(items);
        mockContext.Setup(c => c.InstanceServices).Returns(_serviceProvider);
        return mockContext.Object;
    }

    private (Mock<HttpRequestData>, Mock<HttpResponseData>, MemoryStream) CreateRequestResponseMocks(
        FunctionContext context,
        string? body = null)
    {
        var mockRequest = new Mock<HttpRequestData>(context);
        var mockResponse = new Mock<HttpResponseData>(context);
        var memoryStream = new MemoryStream();

        mockResponse.SetupProperty(r => r.StatusCode);
        mockResponse.SetupProperty(r => r.Headers, new HttpHeadersCollection());
        mockResponse.Setup(r => r.Body).Returns(memoryStream);
        mockRequest.Setup(r => r.CreateResponse()).Returns(mockResponse.Object);

        if (body is not null)
        {
            var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            mockRequest.Setup(r => r.Body).Returns(bodyStream);
        }

        return (mockRequest, mockResponse, memoryStream);
    }

    // ── GET /profile ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_WithAuthenticatedUser_ReturnsOkWithProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedContext(userId);
        var (mockRequest, mockResponse, _) = CreateRequestResponseMocks(context);

        var profile = new ProfileResponse(
            userId, "user@example.com", "User", 1, "テスト家族", "ABCD1234",
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        _mockProfileService.Setup(s => s.GetProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var response = await _sut.GetProfileAsync(mockRequest.Object, context, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockProfileService.Verify(s => s.GetProfileAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedContext(userId);
        var (mockRequest, _, _) = CreateRequestResponseMocks(context);

        _mockProfileService.Setup(s => s.GetProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("ユーザーが見つかりません"));

        // Act
        var response = await _sut.GetProfileAsync(mockRequest.Object, context, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /profile/password ─────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_WithValidRequest_ReturnsOkWithNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedContext(userId);
        var body = JsonSerializer.Serialize(new { currentPassword = "OldPass123!", newPassword = "NewPass456!" });
        var (mockRequest, _, _) = CreateRequestResponseMocks(context, body);

        var authResponse = new AuthResponse("new-access-token", "new-refresh-token", DateTime.UtcNow.AddHours(1));
        _mockProfileService
            .Setup(s => s.ChangePasswordAsync(userId, It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        // Act
        var response = await _sut.ChangePasswordAsync(mockRequest.Object, context, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockProfileService.Verify(
            s => s.ChangePasswordAsync(
                userId,
                It.Is<ChangePasswordRequest>(r =>
                    r.CurrentPassword == "OldPass123!" && r.NewPassword == "NewPass456!"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithMissingBody_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedContext(userId);
        var (mockRequest, _, _) = CreateRequestResponseMocks(context, body: null);
        mockRequest.Setup(r => r.Body).Returns(new MemoryStream());

        // Act
        var response = await _sut.ChangePasswordAsync(mockRequest.Object, context, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _mockProfileService.Verify(
            s => s.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithEmptyPasswords_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedContext(userId);
        var body = JsonSerializer.Serialize(new { currentPassword = "", newPassword = "" });
        var (mockRequest, _, _) = CreateRequestResponseMocks(context, body);

        // Act
        var response = await _sut.ChangePasswordAsync(mockRequest.Object, context, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedContext(userId);
        var body = JsonSerializer.Serialize(new { currentPassword = "WrongPass!", newPassword = "NewPass456!" });
        var (mockRequest, _, _) = CreateRequestResponseMocks(context, body);

        _mockProfileService
            .Setup(s => s.ChangePasswordAsync(userId, It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("パスワード変更に失敗しました"));

        // Act
        var response = await _sut.ChangePasswordAsync(mockRequest.Object, context, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /profile/leave-group ─────────────────────────────────────────

    [Fact]
    public async Task LeaveGroupAsync_WhenGroupMember_ReturnsOkWithNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedContext(userId);
        var (mockRequest, _, _) = CreateRequestResponseMocks(context);

        var authResponse = new AuthResponse("new-access-token", "new-refresh-token", DateTime.UtcNow.AddHours(1));
        _mockProfileService.Setup(s => s.LeaveGroupAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        // Act
        var response = await _sut.LeaveGroupAsync(mockRequest.Object, context, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockProfileService.Verify(s => s.LeaveGroupAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveGroupAsync_WhenNotInGroup_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedContext(userId);
        var (mockRequest, _, _) = CreateRequestResponseMocks(context);

        _mockProfileService.Setup(s => s.LeaveGroupAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("グループに参加していません"));

        // Act
        var response = await _sut.LeaveGroupAsync(mockRequest.Object, context, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LeaveGroupAsync_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedContext(userId);
        var (mockRequest, _, _) = CreateRequestResponseMocks(context);

        _mockProfileService.Setup(s => s.LeaveGroupAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("ユーザーが見つかりません"));

        // Act
        var response = await _sut.LeaveGroupAsync(mockRequest.Object, context, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
