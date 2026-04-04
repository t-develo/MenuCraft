#nullable enable

using System.Net;
using MenuCraft.Api.Dtos;
using MenuCraft.Api.Dtos.Auth;
using MenuCraft.Api.Dtos.Profile;
using MenuCraft.Api.Extensions;
using MenuCraft.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Functions;

public class ProfileFunction
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileFunction> _logger;

    public ProfileFunction(IProfileService profileService, ILogger<ProfileFunction> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    // GET profile
    [Function("GetProfile")]
    public async Task<HttpResponseData> GetProfileAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "profile")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        try
        {
            var profile = await _profileService.GetProfileAsync(userId, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(
                ApiResponse<ProfileResponse>.Ok(profile), cancellationToken);
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return notFoundResponse;
        }
    }

    // PUT profile/password
    [Function("ChangePassword")]
    public async Task<HttpResponseData> ChangePasswordAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "profile/password")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        ChangePasswordRequest? request;
        try
        {
            request = await req.ReadFromJsonAsync<ChangePasswordRequest>(cancellationToken);
        }
        catch (Exception)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("リクエストボディが不正です"), cancellationToken);
            return badResponse;
        }

        if (request is null
            || string.IsNullOrWhiteSpace(request.CurrentPassword)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("現在のパスワードと新しいパスワードは必須です"), cancellationToken);
            return badResponse;
        }

        try
        {
            var authResponse = await _profileService.ChangePasswordAsync(userId, request, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(
                ApiResponse<AuthResponse>.Ok(authResponse), cancellationToken);
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return notFoundResponse;
        }
        catch (InvalidOperationException ex)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return badResponse;
        }
    }

    // POST profile/leave-group
    [Function("LeaveGroup")]
    public async Task<HttpResponseData> LeaveGroupAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "profile/leave-group")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        try
        {
            var authResponse = await _profileService.LeaveGroupAsync(userId, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(
                ApiResponse<AuthResponse>.Ok(authResponse), cancellationToken);
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return notFoundResponse;
        }
        catch (InvalidOperationException ex)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return badResponse;
        }
    }
}
