#nullable enable

namespace MenuCraft.Api.Dtos.Admin;

public record GroupListResponse(
    int Id,
    string Name,
    string InviteCode,
    int MemberCount,
    DateTime CreatedAt);
