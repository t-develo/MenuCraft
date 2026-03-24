---
description: Comprehensive C#/.NET code review for clean code, async correctness, security, and idiomatic patterns. Invokes the dotnet-reviewer agent.
---

# C#/.NET Code Review

This command invokes the **dotnet-reviewer** agent for comprehensive C#/.NET-specific code review.

## What This Command Does

1. **Identify C# Changes**: Find modified `.cs` files via `git diff`
2. **Run Static Analysis**: Execute `dotnet build`, `dotnet format --verify-no-changes`
3. **Security Scan**: Check for SQL injection, hardcoded secrets, missing `[Authorize]`
4. **Async Safety Review**: Analyze async/await patterns and deadlock risks
5. **Idiomatic Code Check**: Verify code follows C# best practices and .NET conventions
6. **Generate Report**: Categorize issues by severity

## When to Use

Use `/dotnet-review` when:
- After writing or modifying C# code
- Before committing C# changes
- Reviewing pull requests with C# code
- Onboarding to a new .NET codebase
- Learning idiomatic C# patterns

## Review Categories

### CRITICAL (Must Fix)
- SQL injection vulnerabilities (string-concatenated queries)
- Missing `[Authorize]` on protected Azure Functions
- Hardcoded connection strings or API keys
- `.Result`/`.Wait()` deadlock patterns
- Empty catch blocks swallowing exceptions

### HIGH (Should Fix)
- Missing type annotations on public methods
- `async void` usage outside event handlers
- Missing CancellationToken parameters
- Sync-over-async patterns
- N+1 EF Core query patterns
- Not using `using` for IDisposable

### MEDIUM (Consider)
- Missing XML doc comments on public APIs
- `Console.WriteLine` instead of `ILogger`
- Magic numbers without named constants
- `DateTime.Now` instead of `DateTime.UtcNow`
- `.Count() > 0` instead of `.Any()`

## Automated Checks Run

```bash
# Compilation check
dotnet build src/api/

# Format check
dotnet format --verify-no-changes src/api/

# Testing
dotnet test

# Coverage
dotnet test --collect:"XPlat Code Coverage"

# Security scan (manual search)
grep -rn "password\|secret\|connectionstring" src/ -i
```

## Example Usage

```text
User: /dotnet-review

Agent:
# C#/.NET Code Review Report

## Files Reviewed
- src/api/Functions/RecipeFunction.cs (modified)
- src/api/Services/RecipeService.cs (modified)

## Build Status
dotnet build: OK (0 errors, 2 warnings)

## Issues Found

[CRITICAL] SQL Injection vulnerability
File: src/api/Services/RecipeService.cs:42
Issue: User input directly interpolated into SQL query
// BAD
var query = $"SELECT * FROM Recipes WHERE Title LIKE '%{searchTerm}%'";

Fix: Use parameterized query or EF Core
// GOOD
var recipes = await _context.Recipes
    .Where(r => r.Title.Contains(searchTerm))
    .ToListAsync(cancellationToken);

[HIGH] Deadlock risk: .Result on async method
File: src/api/Functions/RecipeFunction.cs:18
Issue: Calling .Result in async context causes deadlock
// BAD
var result = _service.GetRecipesAsync().Result;

Fix: Use await
// GOOD
var result = await _service.GetRecipesAsync(cancellationToken);

## Summary
- CRITICAL: 1
- HIGH: 1
- MEDIUM: 0

Recommendation: BLOCK merge until CRITICAL issue is fixed
```

## Approval Criteria

| Status | Condition |
|--------|-----------|
| Approve | No CRITICAL or HIGH issues |
| Warning | Only MEDIUM issues (merge with caution) |
| Block | CRITICAL or HIGH issues found |

## Integration with Other Commands

- Use `/tdd` first to ensure tests pass
- Use `/code-review` for general concerns
- Use `/dotnet-review` before committing C# changes
- Use `/build-fix` if compilation fails

## Azure Functions Specific Checks

The reviewer specifically checks:
- `[Function]` attribute on all function methods
- `[Authorize]` or JWT validation applied to protected endpoints
- Dependency injection configured in `Program.cs`
- `ILogger` injected via constructor
- Proper `HttpResponseData` return types
- No blocking I/O in Azure Functions handlers

## Common Fixes

### Use await instead of .Result
```csharp
// Before
var recipe = _service.GetRecipeAsync(id).Result;

// After
var recipe = await _service.GetRecipeAsync(id, cancellationToken);
```

### Use EF Core instead of raw SQL
```csharp
// Before
var query = $"SELECT * FROM Recipes WHERE FamilyGroupId = {groupId}";
var recipes = _context.Database.ExecuteSqlRaw(query);

// After
var recipes = await _context.Recipes
    .Where(r => r.FamilyGroupId == groupId)
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

### Add CancellationToken
```csharp
// Before
public async Task<Recipe?> GetRecipeAsync(int id)

// After
public async Task<Recipe?> GetRecipeAsync(int id, CancellationToken cancellationToken = default)
```

### Use ILogger instead of Console
```csharp
// Before
Console.WriteLine($"Processing recipe {id}");

// After
_logger.LogInformation("Processing recipe {RecipeId}", id);
```

## Related

- Agent: `.claude/agents/dotnet-reviewer.md`
- Rules: `.claude/rules/dotnet/`
