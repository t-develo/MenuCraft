#nullable enable

namespace MenuCraft.Api.Dtos.Profile;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
