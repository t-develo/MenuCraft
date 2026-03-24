# Test Coverage

Analyze test coverage, identify gaps, and generate missing tests to reach 80%+ coverage.

## Step 1: Detect Test Framework

| Indicator | Coverage Command |
|-----------|-----------------|
| `*.csproj` with xUnit/NUnit | `dotnet test --collect:"XPlat Code Coverage"` |
| `jest.config.*` or `package.json` jest | `npx jest --coverage --coverageReporters=json-summary` |
| `vitest.config.*` | `npx vitest run --coverage` |
| `go.mod` | `go test -coverprofile=coverage.out ./...` |

## Step 2: Run .NET Coverage

```bash
# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate human-readable HTML report (requires reportgenerator tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"./coverage/**/coverage.cobertura.xml" \
  -targetdir:"./coverage-report" \
  -reporttypes:Html

# Open report
xdg-open ./coverage-report/index.html
```

## Step 3: Analyze Coverage Report

1. Run the coverage command
2. Parse the output (XML/JSON/terminal)
3. List files **below 80% coverage**, sorted worst-first
4. For each under-covered file, identify:
   - Untested methods
   - Missing branch coverage (if/else, switch, error paths)
   - Dead code that inflates the denominator

## Step 4: Generate Missing Tests

For each under-covered file, generate tests following this priority:

1. **Happy path** — Core functionality with valid inputs
2. **Error handling** — Invalid inputs, missing data, network failures
3. **Edge cases** — Empty collections, null, boundary values (0, -1, MaxValue)
4. **Branch coverage** — Each if/else, switch case, ternary, guard clause

### Test Generation Rules

- Place tests in the test project: `src/api.Tests/` mirroring the source structure
- Use existing test patterns from the project (xUnit `[Fact]`/`[Theory]`, Moq for mocking)
- Mock external dependencies (EF Core DbContext, HTTP calls, Azure services)
- Each test should be independent — no shared mutable state between tests
- Name tests descriptively: `MethodName_WhenCondition_ExpectedResult`

## Step 5: Verify

1. Run the full test suite — all tests must pass: `dotnet test`
2. Re-run coverage — verify improvement
3. If still below 80%, repeat Step 4 for remaining gaps

## Step 6: Report

Show before/after comparison:

```
Coverage Report
──────────────────────────────
File                              Before  After
RecipeService.cs                  45%     88%
MealPlanGeneratorService.cs       32%     82%
ShoppingListService.cs            61%     85%
──────────────────────────────
Overall:                          57%     85%  (target: 80%)
```

## Focus Areas

- Service classes with complex branching (high cyclomatic complexity)
- Error handlers and catch blocks
- Utility functions used across the codebase
- Azure Functions endpoint handlers (request → response flow)
- Edge cases: null, empty collections, boundary dates, invalid IDs
- Auto-generation algorithm (MealPlanGeneratorService)

## xUnit Patterns for Common Gaps

### Testing null/empty inputs
```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public async Task Method_WithInvalidInput_ThrowsArgumentException(string? input)
{
    await Assert.ThrowsAsync<ArgumentException>(() => _service.Method(input!));
}
```

### Testing error paths
```csharp
[Fact]
public async Task GetRecipeAsync_WhenDbThrows_PropagatesException()
{
    _mockRepo.Setup(r => r.FindByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("DB error"));

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => _service.GetRecipeAsync(1));
}
```
