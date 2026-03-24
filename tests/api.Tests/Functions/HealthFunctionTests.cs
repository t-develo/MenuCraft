using System.Net;
using Azure.Core.Serialization;
using FluentAssertions;
using MenuCraft.Api.Functions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MenuCraft.Api.Tests.Functions;

public class HealthFunctionTests
{
    private readonly HealthFunction _sut;

    public HealthFunctionTests()
    {
        var logger = Mock.Of<ILogger<HealthFunction>>();
        _sut = new HealthFunction(logger);
    }

    [Fact]
    public async Task RunAsync_ReturnsOkWithStatus()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .Configure<WorkerOptions>(o => o.Serializer = new JsonObjectSerializer())
            .BuildServiceProvider();
        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.InstanceServices).Returns(serviceProvider);

        var mockRequest = new Mock<HttpRequestData>(mockContext.Object);
        var mockResponse = new Mock<HttpResponseData>(mockContext.Object);
        var memoryStream = new MemoryStream();

        mockResponse.SetupProperty(r => r.StatusCode);
        mockResponse.SetupProperty(r => r.Headers, new HttpHeadersCollection());
        mockResponse.Setup(r => r.Body).Returns(memoryStream);

        mockRequest
            .Setup(r => r.CreateResponse())
            .Returns(mockResponse.Object);

        // Act
        var response = await _sut.RunAsync(mockRequest.Object, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
