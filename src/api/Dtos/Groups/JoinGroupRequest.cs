using System.ComponentModel.DataAnnotations;

namespace MenuCraft.Api.Dtos.Groups;

public record JoinGroupRequest(
    [Required][MaxLength(20)] string InviteCode
);
