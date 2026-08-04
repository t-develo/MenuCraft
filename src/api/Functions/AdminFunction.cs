#nullable enable

using System.Net;
using MenuCraft.Api.Dtos;
using MenuCraft.Api.Dtos.Admin;
using MenuCraft.Api.Extensions;
using MenuCraft.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Functions;

public class AdminFunction
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminFunction> _logger;

    public AdminFunction(IAdminService adminService, ILogger<AdminFunction> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    // GET admin/users
    [Function("AdminGetUsers")]
    public async Task<HttpResponseData> GetUsersAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "management/users")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        context.RequireAdmin();

        var users = await _adminService.GetAllUsersAsync(cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<IReadOnlyList<UserListResponse>>.Ok(users), cancellationToken);
        return response;
    }

    // PUT admin/users/{userId}/role
    [Function("AdminChangeUserRole")]
    public async Task<HttpResponseData> ChangeUserRoleAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "management/users/{userId}/role")] HttpRequestData req,
        string userId,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        context.RequireAdmin();

        if (!Guid.TryParse(userId, out var userGuid))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail("無効なユーザーIDです"), cancellationToken);
            return badResponse;
        }

        var request = await req.ReadFromJsonAsync<ChangeRoleRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.Role))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail("ロールは必須です"), cancellationToken);
            return badResponse;
        }

        try
        {
            await _adminService.ChangeUserRoleAsync(userGuid, request.Role, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return notFoundResponse;
        }
        catch (ArgumentException ex)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return badResponse;
        }
        catch (InvalidOperationException ex)
        {
            var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
            await conflictResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return conflictResponse;
        }
    }

    // GET admin/groups
    [Function("AdminGetGroups")]
    public async Task<HttpResponseData> GetGroupsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "management/groups")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        context.RequireAdmin();

        var groups = await _adminService.GetAllGroupsAsync(cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<IReadOnlyList<GroupListResponse>>.Ok(groups), cancellationToken);
        return response;
    }

    // PUT admin/groups/{groupId}
    [Function("AdminUpdateGroup")]
    public async Task<HttpResponseData> UpdateGroupAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "management/groups/{groupId}")] HttpRequestData req,
        string groupId,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        context.RequireAdmin();

        if (!int.TryParse(groupId, out var groupIdInt))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail("無効なグループIDです"), cancellationToken);
            return badResponse;
        }

        var request = await req.ReadFromJsonAsync<UpdateGroupRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail("グループ名は必須です"), cancellationToken);
            return badResponse;
        }

        try
        {
            var updated = await _adminService.UpdateGroupNameAsync(groupIdInt, request.Name, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<GroupListResponse>.Ok(updated), cancellationToken);
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return notFoundResponse;
        }
    }

    // DELETE admin/groups/{groupId}
    [Function("AdminDeleteGroup")]
    public async Task<HttpResponseData> DeleteGroupAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "management/groups/{groupId}")] HttpRequestData req,
        string groupId,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        context.RequireAdmin();

        if (!int.TryParse(groupId, out var groupIdInt))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail("無効なグループIDです"), cancellationToken);
            return badResponse;
        }

        try
        {
            await _adminService.DeleteGroupAsync(groupIdInt, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return notFoundResponse;
        }
    }

    // DELETE admin/groups/{groupId}/members/{userId}
    [Function("AdminRemoveMember")]
    public async Task<HttpResponseData> RemoveMemberAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "management/groups/{groupId}/members/{userId}")] HttpRequestData req,
        string groupId,
        string userId,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        context.RequireAdmin();

        if (!int.TryParse(groupId, out var groupIdInt))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail("無効なグループIDです"), cancellationToken);
            return badResponse;
        }

        if (!Guid.TryParse(userId, out var userGuid))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail("無効なユーザーIDです"), cancellationToken);
            return badResponse;
        }

        try
        {
            await _adminService.RemoveMemberFromGroupAsync(groupIdInt, userGuid, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return notFoundResponse;
        }
    }

    // POST admin/groups/{groupId}/invite-code
    [Function("AdminRegenerateInviteCode")]
    public async Task<HttpResponseData> RegenerateInviteCodeAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "management/groups/{groupId}/invite-code")] HttpRequestData req,
        string groupId,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        context.RequireAdmin();

        if (!int.TryParse(groupId, out var groupIdInt))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail("無効なグループIDです"), cancellationToken);
            return badResponse;
        }

        try
        {
            var updated = await _adminService.RegenerateInviteCodeAsync(groupIdInt, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<GroupListResponse>.Ok(updated), cancellationToken);
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return notFoundResponse;
        }
    }
}
