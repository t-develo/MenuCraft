#nullable enable

using MenuCraft.Api.Dtos.Auth;
using MenuCraft.Api.Dtos.Profile;

namespace MenuCraft.Api.Services;

public interface IProfileService
{
    Task<ProfileResponse> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<AuthResponse> LeaveGroupAsync(Guid userId, CancellationToken ct = default);
}
