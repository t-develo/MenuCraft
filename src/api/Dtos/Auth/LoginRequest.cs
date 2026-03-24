using System.ComponentModel.DataAnnotations;

namespace MenuCraft.Api.Dtos.Auth;

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);
