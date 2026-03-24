---
paths:
  - "**/*.cs"
  - "**/*.csproj"
---
# C#/.NET Patterns

> This file extends [common/patterns.md](../common/patterns.md) with C#/.NET specific content.

## Repository Pattern

```csharp
public interface IRecipeRepository
{
    Task<Recipe?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Recipe>> GetByGroupIdAsync(int groupId, CancellationToken ct = default);
    Task<Recipe> CreateAsync(Recipe recipe, CancellationToken ct = default);
    Task<Recipe> UpdateAsync(Recipe recipe, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

## Records as DTOs

```csharp
// Immutable request DTO
public record CreateRecipeRequest(
    string Title,
    string? Url,
    string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<IngredientRequest> Ingredients
);

// Immutable response DTO
public record RecipeResponse(
    int Id,
    string Title,
    string? Url,
    string? ImageUrl,
    IReadOnlyList<string> Tags
);
```

## Service Layer Pattern

```csharp
public interface IRecipeService
{
    Task<RecipeResponse?> GetRecipeAsync(int id, int familyGroupId, CancellationToken ct = default);
    Task<IReadOnlyList<RecipeResponse>> GetRecipesAsync(int familyGroupId, CancellationToken ct = default);
    Task<RecipeResponse> CreateRecipeAsync(int familyGroupId, CreateRecipeRequest request, CancellationToken ct = default);
}

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _repo;
    private readonly ILogger<RecipeService> _logger;

    public RecipeService(IRecipeRepository repo, ILogger<RecipeService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<RecipeResponse?> GetRecipeAsync(int id, int familyGroupId, CancellationToken ct = default)
    {
        var recipe = await _repo.FindByIdAsync(id, ct);
        if (recipe is null || recipe.FamilyGroupId != familyGroupId)
            return null;

        return MapToResponse(recipe);
    }

    private static RecipeResponse MapToResponse(Recipe recipe) => new(
        recipe.Id,
        recipe.Title,
        recipe.Url,
        recipe.ImageUrl,
        recipe.Tags.Select(t => t.Name).ToList()
    );
}
```

## Azure Functions Pattern

```csharp
public class RecipeFunction
{
    private readonly IRecipeService _service;
    private readonly ILogger<RecipeFunction> _logger;

    public RecipeFunction(IRecipeService service, ILogger<RecipeFunction> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("GetRecipes")]
    [Authorize]
    public async Task<HttpResponseData> GetRecipesAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "recipes")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var familyGroupId = req.GetFamilyGroupId(); // from JWT claims
        var recipes = await _service.GetRecipesAsync(familyGroupId, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(recipes, cancellationToken);
        return response;
    }
}
```

## EF Core Patterns

```csharp
// Always use AsNoTracking for read-only queries
var recipes = await _context.Recipes
    .Where(r => r.FamilyGroupId == groupId)
    .Include(r => r.Tags)
    .Include(r => r.Ingredients)
    .AsNoTracking()
    .ToListAsync(cancellationToken);

// Use Select for projections (avoid loading full entities)
var recipeSummaries = await _context.Recipes
    .Where(r => r.FamilyGroupId == groupId)
    .Select(r => new RecipeSummary { Id = r.Id, Title = r.Title })
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

## LINQ Patterns

```csharp
// Prefer LINQ over manual loops
var activeRecipes = recipes
    .Where(r => !r.IsDeleted)
    .OrderBy(r => r.Title)
    .ToList();

// Use FirstOrDefault with null-conditional
var recipe = recipes.FirstOrDefault(r => r.Id == id);

// Prefer Any() over Count() > 0
if (recipes.Any(r => r.Tags.Contains("vegetarian"))) { ... }
```

## Reference

See agent: `agents/dotnet-reviewer.md` for comprehensive C#/.NET review guidelines.
