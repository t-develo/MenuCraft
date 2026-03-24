---
paths:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/local.settings.json"
  - "**/appsettings*.json"
---
# C#/.NET Security

> This file extends [common/security.md](../common/security.md) with C#/.NET specific content.

## Secret Management

```csharp
// NEVER: Hardcoded secrets
var connectionString = "Server=myserver;Password=abc123";
var jwtSecret = "my-super-secret-key";

// ALWAYS: Use IConfiguration (backed by Azure Key Vault / App Configuration)
var connectionString = _configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not configured.");

var jwtSecret = _configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT secret not configured.");
```

## Configuration Security

- `local.settings.json` MUST be in `.gitignore`
- Production secrets stored in **Azure Key Vault** or **Azure App Configuration**
- Use **Managed Identity** to access Key Vault (no credentials needed)
- Never put real secrets in `appsettings.json` committed to source control

```json
// appsettings.json (safe to commit - no real secrets)
{
  "ConnectionStrings": {
    "Default": ""  // Filled by Azure App Configuration / Key Vault reference
  },
  "Jwt": {
    "Issuer": "https://menucraft.azurewebsites.net",
    "Audience": "menucraft-api"
    // Secret NOT here - loaded from Key Vault
  }
}
```

## Parameterized Queries

```csharp
// NEVER: String interpolation in SQL
var query = $"SELECT * FROM Recipes WHERE Title LIKE '%{searchTerm}%'";
_context.Database.ExecuteSqlRaw(query); // SQL injection!

// ALWAYS: Use EF Core or parameterized queries
var recipes = await _context.Recipes
    .Where(r => r.Title.Contains(searchTerm))
    .ToListAsync(cancellationToken);

// If raw SQL is needed, use FromSqlInterpolated (safe) or parameterized
var recipes = await _context.Recipes
    .FromSqlInterpolated($"SELECT * FROM Recipes WHERE Title LIKE {$"%{searchTerm}%"}")
    .ToListAsync(cancellationToken);
```

## JWT Authentication

```csharp
// Validate JWT on all protected endpoints
[Function("GetRecipes")]
[Authorize]  // This attribute must be present
public async Task<HttpResponseData> GetRecipesAsync(...)

// Verify family group isolation — user can only access their own group's data
var userGroupId = int.Parse(req.HttpContext.User.FindFirst("familyGroupId")?.Value
    ?? throw new UnauthorizedException());
```

## Input Validation

```csharp
// Use DataAnnotations or FluentValidation
public record CreateRecipeRequest(
    [Required][MaxLength(200)] string Title,
    [Url] string? Url,
    [MaxLength(2000)] string? Description
);

// Validate in function handler
if (!request.IsValid(out var errors))
    return req.CreateValidationErrorResponse(errors);
```

## Security Scanning

```bash
# Check for vulnerable NuGet packages
dotnet list src/api/ package --vulnerable

# Search for hardcoded secrets
grep -rn "password\|secret\|connectionstring\|apikey" src/ \
  --include="*.cs" --include="*.json" -i \
  | grep -v "local.settings.json\|example\|test\|TODO"
```

## Reference

See agent: `agents/security-reviewer.md` for comprehensive security review guidelines.
