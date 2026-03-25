using System.Net;
using System.Text.RegularExpressions;
using MenuCraft.Api.Dtos.Recipes;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Services;

public class OgpService : IOgpService
{
    private const int MaxResponseBytes = 1024 * 1024; // 1MB

    private static readonly HashSet<string> AllowedSchemes =
        new(StringComparer.OrdinalIgnoreCase) { "http", "https" };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OgpService> _logger;

    public OgpService(IHttpClientFactory httpClientFactory, ILogger<OgpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<OgpResponse?> FetchOgpAsync(string url, CancellationToken ct = default)
    {
        if (!TryValidateUrl(url, out var uri))
        {
            _logger.LogDebug("URL rejected (invalid or blocked): {Url}", url);
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("OgpClient");
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("OGP fetch returned non-success status {Status} for {Url}", response.StatusCode, url);
                return null;
            }

            var html = await ReadWithSizeLimitAsync(response, MaxResponseBytes, ct);
            if (html is null)
            {
                _logger.LogDebug("OGP response exceeded size limit for {Url}", url);
                return null;
            }

            return ParseOgp(html);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to fetch OGP for URL: {Url}", url);
            return null;
        }
    }

    private static bool TryValidateUrl(string url, out Uri? uri)
    {
        uri = null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            return false;

        if (!AllowedSchemes.Contains(parsed.Scheme))
            return false;

        if (IsPrivateHost(parsed.Host))
            return false;

        uri = parsed;
        return true;
    }

    internal static bool IsPrivateHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return true;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!IPAddress.TryParse(host, out var ip))
            return false; // DNS names are allowed (not resolving to validate)

        var bytes = ip.GetAddressBytes();

        // Block IPv6 private/loopback ranges conservatively
        if (bytes.Length != 4)
            return true;

        return
            bytes[0] == 0 ||                                                // 0.0.0.0/8
            bytes[0] == 127 ||                                              // 127.0.0.0/8 loopback
            bytes[0] == 10 ||                                               // 10.0.0.0/8
            (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||       // 172.16.0.0/12
            (bytes[0] == 192 && bytes[1] == 168) ||                        // 192.168.0.0/16
            (bytes[0] == 169 && bytes[1] == 254);                          // 169.254.0.0/16 link-local (IMDS)
    }

    private static async Task<string?> ReadWithSizeLimitAsync(
        HttpResponseMessage response, int maxBytes, CancellationToken ct)
    {
        // Buffer is maxBytes + 1 so we can detect when the response exceeds the limit.
        var buffer = new byte[maxBytes + 1];
        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        int totalRead = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), ct)) > 0)
        {
            totalRead += read;
            if (totalRead >= buffer.Length)
                break; // definitely over the limit — stop reading
        }

        if (totalRead > maxBytes)
            return null;

        // Detect encoding from Content-Type or default to UTF-8
        var charset = response.Content.Headers.ContentType?.CharSet;
        var encoding = TryGetEncoding(charset) ?? System.Text.Encoding.UTF8;

        return encoding.GetString(buffer, 0, totalRead);
    }

    private static System.Text.Encoding? TryGetEncoding(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset))
            return null;
        try { return System.Text.Encoding.GetEncoding(charset); }
        catch { return null; }
    }

    private static OgpResponse ParseOgp(string html)
    {
        var title = ExtractOgMetaContent(html, "og:title");
        var imageUrl = ExtractOgMetaContent(html, "og:image");
        var description = ExtractOgMetaContent(html, "og:description");

        // Fallback to <title> tag when og:title is absent
        if (title is null)
        {
            var titleMatch = Regex.Match(html,
                @"<title[^>]*>\s*(.*?)\s*</title>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (titleMatch.Success)
                title = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());
        }

        return new OgpResponse(
            NullIfEmpty(title),
            IsValidHttpUrl(imageUrl) ? imageUrl?.Trim() : null,
            NullIfEmpty(description));
    }

    /// <summary>
    /// Extracts the content attribute value of a meta tag with the given og: property,
    /// handling both attribute orderings: property-first and content-first.
    /// </summary>
    private static string? ExtractOgMetaContent(string html, string property)
    {
        // Matches: <meta ... property="og:xxx" ... content="VALUE" ... />
        var pattern1 = $@"<meta\b[^>]*\bproperty=[""']{Regex.Escape(property)}[""'][^>]*\bcontent=[""']([^""']*)[""'][^>]*/?>";
        // Matches: <meta ... content="VALUE" ... property="og:xxx" ... />
        var pattern2 = $@"<meta\b[^>]*\bcontent=[""']([^""']*)[""'][^>]*\bproperty=[""']{Regex.Escape(property)}[""'][^>]*/?>";

        var match = Regex.Match(html, pattern1, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
            match = Regex.Match(html, pattern2, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!match.Success)
            return null;

        return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool IsValidHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u)
               && (u.Scheme == "https" || u.Scheme == "http");
    }
}
