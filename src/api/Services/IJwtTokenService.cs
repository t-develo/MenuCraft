using MenuCraft.Api.Models;

namespace MenuCraft.Api.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, string role);
    string GenerateRefreshToken();
}
