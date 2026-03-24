---
paths:
  - "**/*.cs"
  - "**/*.csproj"
---
# C#/.NET Coding Style

> This file extends [common/coding-style.md](../common/coding-style.md) with C#/.NET specific content.

## Standards

- Follow **Microsoft C# Coding Conventions**
- Use **type annotations** on all public method signatures
- Use **nullable reference types** (`#nullable enable`) in all new files

## Immutability

Prefer immutable data structures:

```csharp
// Immutable record for DTOs
public record CreateRecipeRequest(
    string Title,
    string? Url,
    string? Description,
    IReadOnlyList<string> Tags
);

// Init-only properties for models
public class Recipe
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int FamilyGroupId { get; init; }
}

// Immutable update pattern
var updated = recipe with { Title = newTitle };
```

## Naming Conventions

- **PascalCase**: Classes, interfaces, methods, properties, public fields
- **camelCase**: Local variables, parameters, private fields (with `_` prefix)
- **IPascalCase**: Interfaces (e.g., `IRecipeService`)
- **UPPER_SNAKE_CASE**: Constants
- **Async suffix**: Async methods must end with `Async` (e.g., `GetRecipeAsync`)

## Formatting

- Use `dotnet format` for code formatting
- 4 spaces for indentation (no tabs)
- Opening braces on same line for methods/classes (Allman style via `dotnet format`)
- Use `var` when the type is obvious from context

## Async/Await

Always use `async`/`await` properly:

```csharp
// WRONG: Sync-over-async (deadlock risk)
var result = GetDataAsync().Result;

// CORRECT: Fully async
var result = await GetDataAsync(cancellationToken);

// WRONG: async void (unhandled exceptions)
public async void ProcessAsync() { ... }

// CORRECT: async Task
public async Task ProcessAsync() { ... }
```

## Error Handling

```csharp
// WRONG: Swallowing exceptions
try { await DoSomethingAsync(); }
catch (Exception) { }

// CORRECT: Log and handle or rethrow
try { await DoSomethingAsync(); }
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to do something");
    throw; // or return appropriate error response
}
```

## Reference

See rules: `rules/dotnet/patterns.md` for comprehensive C#/.NET idioms and patterns.
