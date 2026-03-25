using System.Net;
using System.Net.Http;
using FluentAssertions;
using MenuCraft.Api.Dtos.Recipes;
using MenuCraft.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MenuCraft.Api.Tests.Services;

public class OgpServiceTests
{
    private static OgpService CreateService(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        factory.Setup(f => f.CreateClient("OgpClient")).Returns(client);
        return new OgpService(factory.Object, NullLogger<OgpService>.Instance);
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(
        HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "text/html")
            });
        return handler;
    }

    // --- URL Validation ---

    [Fact]
    public async Task FetchOgpAsync_WithInvalidUrl_ReturnsNull()
    {
        var service = CreateService(new Mock<HttpMessageHandler>().Object);

        var result = await service.FetchOgpAsync("not-a-url");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchOgpAsync_WithFtpScheme_ReturnsNull()
    {
        var service = CreateService(new Mock<HttpMessageHandler>().Object);

        var result = await service.FetchOgpAsync("ftp://example.com/file");

        result.Should().BeNull();
    }

    // --- SSRF Protection ---

    [Theory]
    [InlineData("http://localhost/page")]
    [InlineData("http://127.0.0.1/page")]
    [InlineData("http://10.0.0.1/page")]
    [InlineData("http://10.255.255.255/page")]
    [InlineData("http://172.16.0.1/page")]
    [InlineData("http://172.31.255.255/page")]
    [InlineData("http://192.168.0.1/page")]
    [InlineData("http://192.168.255.255/page")]
    [InlineData("http://169.254.169.254/latest/meta-data/")] // Azure/AWS IMDS endpoint (SSRF target)
    [InlineData("http://169.254.0.1/page")]                  // link-local range
    [InlineData("http://0.0.0.0/page")]                      // 0.0.0.0/8
    public async Task FetchOgpAsync_WithPrivateIp_ReturnsNull(string privateUrl)
    {
        var service = CreateService(new Mock<HttpMessageHandler>().Object);

        var result = await service.FetchOgpAsync(privateUrl);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchOgpAsync_WithPublicIp_AllowsRequest()
    {
        var html = "<html><head><title>Test</title></head></html>";
        var handler = CreateMockHandler(HttpStatusCode.OK, html);
        var service = CreateService(handler.Object);

        var result = await service.FetchOgpAsync("http://8.8.8.8/page");

        result.Should().NotBeNull();
    }

    // --- OGP Parsing (Happy path) ---

    [Fact]
    public async Task FetchOgpAsync_WithFullOgpTags_ReturnsAllFields()
    {
        var html = """
            <html>
            <head>
              <meta property="og:title" content="美味しいカレーライス" />
              <meta property="og:image" content="https://example.com/image.jpg" />
              <meta property="og:description" content="本格的なカレーレシピです" />
            </head>
            </html>
            """;
        var handler = CreateMockHandler(HttpStatusCode.OK, html);
        var service = CreateService(handler.Object);

        var result = await service.FetchOgpAsync("https://example.com/recipe");

        result.Should().NotBeNull();
        result!.Title.Should().Be("美味しいカレーライス");
        result.ImageUrl.Should().Be("https://example.com/image.jpg");
        result.Description.Should().Be("本格的なカレーレシピです");
    }

    [Fact]
    public async Task FetchOgpAsync_WithOgpTagsInReverseAttributeOrder_ReturnsFields()
    {
        // content comes before property in attribute order
        var html = """
            <html>
            <head>
              <meta content="逆順タイトル" property="og:title" />
              <meta content="https://example.com/img.png" property="og:image" />
            </head>
            </html>
            """;
        var handler = CreateMockHandler(HttpStatusCode.OK, html);
        var service = CreateService(handler.Object);

        var result = await service.FetchOgpAsync("https://example.com/recipe");

        result.Should().NotBeNull();
        result!.Title.Should().Be("逆順タイトル");
        result.ImageUrl.Should().Be("https://example.com/img.png");
    }

    [Fact]
    public async Task FetchOgpAsync_WithNoOgpTitle_FallsBackToTitleTag()
    {
        var html = """
            <html>
            <head>
              <title>ページタイトル</title>
              <meta property="og:image" content="https://example.com/img.jpg" />
            </head>
            </html>
            """;
        var handler = CreateMockHandler(HttpStatusCode.OK, html);
        var service = CreateService(handler.Object);

        var result = await service.FetchOgpAsync("https://example.com/recipe");

        result.Should().NotBeNull();
        result!.Title.Should().Be("ページタイトル");
        result.ImageUrl.Should().Be("https://example.com/img.jpg");
    }

    [Fact]
    public async Task FetchOgpAsync_WithNoOgpTagsAndNoTitle_ReturnsNullFields()
    {
        var html = "<html><head></head><body>No meta</body></html>";
        var handler = CreateMockHandler(HttpStatusCode.OK, html);
        var service = CreateService(handler.Object);

        var result = await service.FetchOgpAsync("https://example.com/recipe");

        result.Should().NotBeNull();
        result!.Title.Should().BeNull();
        result.ImageUrl.Should().BeNull();
        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task FetchOgpAsync_WithJavaScriptImageUrl_ReturnsNullImageUrl()
    {
        var html = """
            <html>
            <head>
              <meta property="og:title" content="テスト" />
              <meta property="og:image" content="javascript:alert(1)" />
            </head>
            </html>
            """;
        var handler = CreateMockHandler(HttpStatusCode.OK, html);
        var service = CreateService(handler.Object);

        var result = await service.FetchOgpAsync("https://example.com/recipe");

        result.Should().NotBeNull();
        result!.Title.Should().Be("テスト");
        result.ImageUrl.Should().BeNull(); // javascript: scheme must be rejected
    }

    [Fact]
    public async Task FetchOgpAsync_WithDataUriImageUrl_ReturnsNullImageUrl()
    {
        var html = """
            <html>
            <head>
              <meta property="og:image" content="data:image/png;base64,abc123" />
            </head>
            </html>
            """;
        var handler = CreateMockHandler(HttpStatusCode.OK, html);
        var service = CreateService(handler.Object);

        var result = await service.FetchOgpAsync("https://example.com/recipe");

        result!.ImageUrl.Should().BeNull(); // data: scheme must be rejected
    }

    // --- Error handling ---

    [Fact]
    public async Task FetchOgpAsync_WhenHttpFails_ReturnsNull()
    {
        var handler = CreateMockHandler(HttpStatusCode.NotFound, "");
        var service = CreateService(handler.Object);

        var result = await service.FetchOgpAsync("https://example.com/recipe");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchOgpAsync_WhenHttpRequestExceptionThrown_ReturnsNull()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var service = CreateService(handler.Object);

        var result = await service.FetchOgpAsync("https://example.com/recipe");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchOgpAsync_WhenResponseExceedsSizeLimit_ReturnsNull()
    {
        // Build content > 1MB
        var largContent = new string('a', 1024 * 1024 + 100);
        var handler = CreateMockHandler(HttpStatusCode.OK, largContent);
        var service = CreateService(handler.Object);

        var result = await service.FetchOgpAsync("https://example.com/recipe");

        result.Should().BeNull();
    }
}
