using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace MenuCraft.Api.Extensions;

public static class HttpRequestDataExtensions
{
    public static ClaimsPrincipal GetUser(this FunctionContext context)
    {
        if (context.Items.TryGetValue("User", out var user) && user is ClaimsPrincipal principal)
        {
            return principal;
        }

        throw new UnauthorizedAccessException("User not authenticated.");
    }

    public static Guid GetUserId(this FunctionContext context)
    {
        var principal = context.GetUser();
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User ID claim not found.");

        return Guid.Parse(userIdClaim);
    }

    public static int? GetFamilyGroupId(this FunctionContext context)
    {
        var principal = context.GetUser();
        var groupIdClaim = principal.FindFirst("familyGroupId")?.Value;

        if (string.IsNullOrEmpty(groupIdClaim))
        {
            return null;
        }

        return int.Parse(groupIdClaim);
    }

    public static int RequireFamilyGroupId(this FunctionContext context)
    {
        return context.GetFamilyGroupId()
            ?? throw new UnauthorizedAccessException("User is not a member of any family group.");
    }
}
