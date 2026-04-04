using System.Net;
using MenuCraft.Api.Dtos;
using MenuCraft.Api.Dtos.Groups;
using MenuCraft.Api.Extensions;
using MenuCraft.Api.Models;
using MenuCraft.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

#nullable enable

namespace MenuCraft.Api.Functions;

public class GroupFunction
{
    private readonly IGroupService _groupService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<GroupFunction> _logger;

    public GroupFunction(
        IGroupService groupService,
        IJwtTokenService jwtTokenService,
        UserManager<User> userManager,
        ILogger<GroupFunction> logger)
    {
        _groupService = groupService;
        _jwtTokenService = jwtTokenService;
        _userManager = userManager;
        _logger = logger;
    }

    private async Task<string> GetUserRoleAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? "User";
    }

    [Function("CreateGroup")]
    public async Task<HttpResponseData> CreateGroupAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "groups")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var request = await req.ReadFromJsonAsync<CreateGroupRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("グループ名は必須です"), cancellationToken);
            return badResponse;
        }

        try
        {
            var group = await _groupService.CreateGroupAsync(userId, request, cancellationToken);

            // Return a fresh token that encodes the new familyGroupId claim.
            // GroupService.CreateGroupAsync already validates user exists; null here is unreachable.
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("User not found after group creation.");
            var role = await GetUserRoleAsync(user);
            var newToken = _jwtTokenService.GenerateAccessToken(user, role);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(
                ApiResponse<GroupWithTokenResponse>.Ok(new GroupWithTokenResponse(
                    group.Id,
                    group.Name,
                    group.InviteCode,
                    newToken,
                    DateTimeOffset.UtcNow.AddHours(1))),
                cancellationToken);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
            await conflictResponse.WriteAsJsonAsync(
                ApiResponse.Fail(ex.Message), cancellationToken);
            return conflictResponse;
        }
    }

    [Function("JoinGroup")]
    public async Task<HttpResponseData> JoinGroupAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "groups/join")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var request = await req.ReadFromJsonAsync<JoinGroupRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.InviteCode))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("招待コードは必須です"), cancellationToken);
            return badResponse;
        }

        try
        {
            var group = await _groupService.JoinGroupAsync(userId, request, cancellationToken);

            // Return a fresh token that encodes the new familyGroupId claim.
            // GroupService.JoinGroupAsync already validates user exists; null here is unreachable.
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("User not found after joining group.");
            var role = await GetUserRoleAsync(user);
            var newToken = _jwtTokenService.GenerateAccessToken(user, role);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(
                ApiResponse<GroupWithTokenResponse>.Ok(new GroupWithTokenResponse(
                    group.Id,
                    group.Name,
                    group.InviteCode,
                    newToken,
                    DateTimeOffset.UtcNow.AddHours(1))),
                cancellationToken);
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(
                ApiResponse.Fail(ex.Message), cancellationToken);
            return notFoundResponse;
        }
        catch (InvalidOperationException ex)
        {
            var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
            await conflictResponse.WriteAsJsonAsync(
                ApiResponse.Fail(ex.Message), cancellationToken);
            return conflictResponse;
        }
    }

    [Function("GetGroupMembers")]
    public async Task<HttpResponseData> GetMembersAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "groups/members")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var familyGroupId = context.RequireFamilyGroupId();

        var members = await _groupService.GetMembersAsync(familyGroupId, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<IReadOnlyList<MemberResponse>>.Ok(members), cancellationToken);
        return response;
    }
}
