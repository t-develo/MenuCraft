---
paths:
  - "**/*.cs"
  - "**/*.csproj"
---
# C#/.NET Testing

> This file extends [common/testing.md](../common/testing.md) with C#/.NET specific content.

## Framework

Use **xUnit** as the primary testing framework (NUnit is acceptable if the project already uses it).

## Required Packages

```xml
<!-- src/api.Tests/api.Tests.csproj -->
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.*" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
```

## Coverage

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML report
reportgenerator \
  -reports:"./coverage/**/coverage.cobertura.xml" \
  -targetdir:"./coverage-report" \
  -reporttypes:Html

# Quick coverage summary in terminal
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

## Test Organization

Use xUnit attributes for test categorization:

```csharp
using Xunit;
using FluentAssertions;
using Moq;

// Unit test - fast, no external dependencies
public class RecipeServiceTests
{
    [Fact]
    public async Task GetRecipeAsync_WithValidId_ReturnsRecipe()
    {
        // Arrange
        var mockRepo = new Mock<IRecipeRepository>();
        mockRepo.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Recipe { Id = 1, Title = "Pasta", FamilyGroupId = 42 });

        var service = new RecipeService(mockRepo.Object, Mock.Of<ILogger<RecipeService>>());

        // Act
        var result = await service.GetRecipeAsync(1, familyGroupId: 42);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Pasta");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetRecipeAsync_WithInvalidId_ThrowsArgumentException(int invalidId)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetRecipeAsync(invalidId, familyGroupId: 1));
    }
}

// Integration test - uses EF Core in-memory database
public class RecipeRepositoryIntegrationTests : IDisposable
{
    private readonly AppDbContext _context;

    public RecipeRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_WithValidRecipe_PersistsToDatabase()
    {
        // ... test using in-memory DB
    }

    public void Dispose() => _context.Dispose();
}
```

## Mocking Guidelines

```csharp
// Mock interfaces, not concrete classes
var mockRepo = new Mock<IRecipeRepository>();

// Setup return values
mockRepo.Setup(r => r.FindByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new Recipe { ... });

// Setup to throw exceptions
mockRepo.Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>()))
    .ThrowsAsync(new KeyNotFoundException());

// Verify calls were made
mockRepo.Verify(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
```

## Reference

See agent: `agents/tdd-guide.md` for detailed TDD patterns and guidance.
