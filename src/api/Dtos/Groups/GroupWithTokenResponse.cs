namespace MenuCraft.Api.Dtos.Groups;

/// <summary>
/// Returned after a user creates or joins a group.
/// Includes a fresh access token that encodes the new familyGroupId claim.
/// </summary>
public record GroupWithTokenResponse(
    int GroupId,
    string GroupName,
    string InviteCode,
    string AccessToken,
    DateTimeOffset ExpiresAt
);
