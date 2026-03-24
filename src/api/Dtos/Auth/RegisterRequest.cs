using System.ComponentModel.DataAnnotations;

namespace MenuCraft.Api.Dtos.Auth;

public record RegisterRequest(
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MinLength(8)][MaxLength(128)] string Password
);
