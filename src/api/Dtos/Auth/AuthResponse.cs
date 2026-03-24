namespace MenuCraft.Api.Dtos.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);
