using System.ComponentModel.DataAnnotations;

namespace MenuCraft.Api.Dtos.Groups;

public record CreateGroupRequest(
    [Required][MaxLength(100)] string Name
);
