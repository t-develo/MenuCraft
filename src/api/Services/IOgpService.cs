using MenuCraft.Api.Dtos.Recipes;

namespace MenuCraft.Api.Services;

public interface IOgpService
{
    /// <summary>
    /// Fetches OGP metadata from the given URL.
    /// Returns null if the URL is invalid, blocked (SSRF), or the request fails.
    /// </summary>
    Task<OgpResponse?> FetchOgpAsync(string url, CancellationToken ct = default);
}
