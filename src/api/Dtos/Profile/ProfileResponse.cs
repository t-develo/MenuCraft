#nullable enable

namespace MenuCraft.Api.Dtos.Profile;

public record ProfileResponse(
    Guid Id,
    string Email,
    string Role,
    int? FamilyGroupId,
    string? GroupName,
    string? InviteCode,
    DateTime CreatedAt
);
