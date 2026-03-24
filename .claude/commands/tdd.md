---
description: Enforce test-driven development workflow. Scaffold interfaces, generate tests FIRST, then implement minimal code to pass. Ensure 80%+ coverage.
---

# TDD Command

This command invokes the **tdd-guide** agent to enforce test-driven development methodology.

## What This Command Does

1. **Scaffold Interfaces** - Define types/interfaces/models first
2. **Generate Tests First** - Write failing tests (RED)
3. **Implement Minimal Code** - Write just enough to pass (GREEN)
4. **Refactor** - Improve code while keeping tests green (REFACTOR)
5. **Verify Coverage** - Ensure 80%+ test coverage

## When to Use

Use `/tdd` when:
- Implementing new features
- Adding new functions/services
- Fixing bugs (write test that reproduces bug first)
- Refactoring existing code
- Building critical business logic

## How It Works

The tdd-guide agent will:

1. **Define interfaces/models** for inputs/outputs
2. **Write tests that will FAIL** (because code doesn't exist yet)
3. **Run tests** and verify they fail for the right reason
4. **Write minimal implementation** to make tests pass
5. **Run tests** and verify they pass
6. **Refactor** code while keeping tests green
7. **Check coverage** and add more tests if below 80%

## TDD Cycle

```
RED → GREEN → REFACTOR → REPEAT

RED:      Write a failing xUnit test
GREEN:    Write minimal code to pass
REFACTOR: Improve code, keep tests passing
REPEAT:   Next feature/scenario
```

## Test Framework Commands

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~RecipeServiceTests"

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"

# Generate coverage report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

## Example TDD Session

```
User: /tdd I need a service to auto-generate a weekly meal plan

Agent (tdd-guide):
# TDD Session: MealPlan Auto-Generator

## Step 1: Define Interface (SCAFFOLD)

// src/api/Services/IMealPlanGeneratorService.cs
public interface IMealPlanGeneratorService
{
    Task<IReadOnlyList<MealPlan>> GenerateWeeklyPlanAsync(
        int familyGroupId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
}

## Step 2: Write Failing Test (RED)

// src/api.Tests/Services/MealPlanGeneratorServiceTests.cs
public class MealPlanGeneratorServiceTests
{
    [Fact]
    public async Task GenerateWeeklyPlanAsync_WithEnoughRecipes_Returns14Meals()
    {
        // Arrange
        var mockRepo = new Mock<IRecipeRepository>();
        mockRepo.Setup(r => r.GetByGroupIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRecipes(20));
        var service = new MealPlanGeneratorService(mockRepo.Object);

        // Act
        var result = await service.GenerateWeeklyPlanAsync(1, new DateOnly(2026, 3, 23));

        // Assert
        result.Should().HaveCount(14); // 7 days x 2 meals (lunch + dinner)
    }
}

## Step 3: Run Tests - Verify FAIL
dotnet test → FAIL: MealPlanGeneratorService not found

## Step 4: Implement Minimal Code (GREEN)
[minimal implementation to make test pass]

## Step 5: Run Tests - Verify PASS
dotnet test → PASS: 1 test passed
```

## TDD Best Practices

**DO:**
- Write the test FIRST, before any implementation
- Run tests and verify they FAIL before implementing
- Write minimal code to make tests pass
- Refactor only after tests are green
- Add edge cases and error scenarios
- Aim for 80%+ coverage (100% for critical code)

**DON'T:**
- Write implementation before tests
- Skip running tests after each change
- Write too much code at once
- Ignore failing tests
- Test implementation details (test behavior)
- Use real database in unit tests (use mock/in-memory)

## Coverage Requirements

- **80% minimum** for all code
- **100% required** for:
  - Business logic (meal plan generation algorithm)
  - Authentication/authorization logic
  - Shopping list calculation
  - Security-critical code

## Important Notes

**MANDATORY**: Tests must be written BEFORE implementation. The TDD cycle is:

1. **RED** - Write failing test
2. **GREEN** - Implement to pass
3. **REFACTOR** - Improve code

Never skip the RED phase. Never write code before tests.

## Integration with Other Commands

- Use `/plan` first to understand what to build
- Use `/tdd` to implement with tests
- Use `/build-fix` if build errors occur
- Use `/code-review` to review implementation
- Use `/test-coverage` to verify coverage

## Related Agents

This command invokes the `tdd-guide` agent.

For manual installs, the source file lives at:
`.claude/agents/tdd-guide.md`
