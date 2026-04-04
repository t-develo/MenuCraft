#nullable enable

namespace MenuCraft.Api.Dtos.Admin;

public record UserListResponse(
    Guid Id,
    string Email,
    string Role,
    int? FamilyGroupId,
    string? GroupName,
    DateTime CreatedAt);
