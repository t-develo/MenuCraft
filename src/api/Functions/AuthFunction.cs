using System.Net;
using MenuCraft.Api.Dtos;
using MenuCraft.Api.Dtos.Auth;
using MenuCraft.Api.Models;
using MenuCraft.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Functions;

public class AuthFunction
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthFunction> _logger;

    public AuthFunction(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        ILogger<AuthFunction> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
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
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

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
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

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

        if (!_jwtTokenService.ValidateRefreshToken(request.RefreshToken))
        {
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(
                ApiResponse.Fail("無効なリフレッシュトークンです"), cancellationToken);
            return unauthorizedResponse;
        }

        // In a production system, we would look up the refresh token in the database
        // to find the associated user. For MVP, we return a generic error since
        // refresh token storage is not yet implemented.
        var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
        await errorResponse.WriteAsJsonAsync(
            ApiResponse.Fail("リフレッシュトークンの検証に失敗しました。再ログインしてください"), cancellationToken);
        return errorResponse;
    }
}
