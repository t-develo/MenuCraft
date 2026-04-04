#nullable enable

using System.Net;
using MenuCraft.Api.Dtos;
using MenuCraft.Api.Dtos.Auth;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Functions;

public class AuthFunction
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthFunction> _logger;

    public AuthFunction(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration,
        ILogger<AuthFunction> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
        _logger = logger;
    }

    [Function("AuthRegister")]
    public async Task<HttpResponseData> RegisterAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var request = await req.ReadFromJsonAsync<RegisterRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("メールアドレスとパスワードは必須です"), cancellationToken);
            return badResponse;
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
            await conflictResponse.WriteAsJsonAsync(
                ApiResponse.Fail("このメールアドレスは既に登録されています"), cancellationToken);
            return conflictResponse;
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, errors);

            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(
                ApiResponse.Fail($"登録に失敗しました: {errors}"), cancellationToken);
            return errorResponse;
        }

        _logger.LogInformation("User registered: {Email}", request.Email);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = await CreateAndSaveRefreshTokenAsync(user.Id, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(
            ApiResponse<AuthResponse>.Ok(new AuthResponse(
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddHours(1))),
            cancellationToken);
        return response;
    }

    [Function("AuthLogin")]
    public async Task<HttpResponseData> LoginAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var request = await req.ReadFromJsonAsync<LoginRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("メールアドレスとパスワードは必須です"), cancellationToken);
            return badResponse;
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await notFoundResponse.WriteAsJsonAsync(
                ApiResponse.Fail("メールアドレスまたはパスワードが正しくありません"), cancellationToken);
            return notFoundResponse;
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);

            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(
                ApiResponse.Fail("メールアドレスまたはパスワードが正しくありません"), cancellationToken);
            return unauthorizedResponse;
        }

        _logger.LogInformation("User logged in: {Email}", request.Email);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = await CreateAndSaveRefreshTokenAsync(user.Id, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<AuthResponse>.Ok(new AuthResponse(
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddHours(1))),
            cancellationToken);
        return response;
    }

    [Function("AuthRefresh")]
    public async Task<HttpResponseData> RefreshAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/refresh")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var request = await req.ReadFromJsonAsync<RefreshRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("リフレッシュトークンは必須です"), cancellationToken);
            return badResponse;
        }

        var storedToken = await _refreshTokenRepository.FindByTokenAsync(request.RefreshToken, cancellationToken);

        if (storedToken is null || !storedToken.IsActive || storedToken.User is null)
        {
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(
                ApiResponse.Fail("無効なリフレッシュトークンです"), cancellationToken);
            return unauthorizedResponse;
        }

        // トークンローテーション: 旧トークンを失効させ、新しいトークンペアを発行
        await _refreshTokenRepository.RevokeAsync(storedToken, cancellationToken);

        var user = storedToken.User;
        // User エンティティの最新情報（FamilyGroupId 等）を取得
        var latestUser = await _userManager.FindByIdAsync(user.Id.ToString());
        if (latestUser is null)
        {
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(
                ApiResponse.Fail("ユーザーが見つかりません"), cancellationToken);
            return unauthorizedResponse;
        }

        var newAccessToken = _jwtTokenService.GenerateAccessToken(latestUser);
        var newRefreshToken = await CreateAndSaveRefreshTokenAsync(latestUser.Id, cancellationToken);

        _logger.LogInformation("Refresh token rotated for user {UserId}", latestUser.Id);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<AuthResponse>.Ok(new AuthResponse(
                newAccessToken,
                newRefreshToken,
                DateTime.UtcNow.AddHours(1))),
            cancellationToken);
        return response;
    }

    private async Task<string> CreateAndSaveRefreshTokenAsync(Guid userId, CancellationToken ct)
    {
        var expirationDays = int.Parse(
            _configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");

        var tokenValue = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = tokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
        };

        await _refreshTokenRepository.CreateAsync(refreshToken, ct);
        return tokenValue;
    }
}
