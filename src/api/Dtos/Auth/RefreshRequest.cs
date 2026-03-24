using System.ComponentModel.DataAnnotations;

namespace MenuCraft.Api.Dtos.Auth;

public record RefreshRequest(
    [Required] string RefreshToken
);
