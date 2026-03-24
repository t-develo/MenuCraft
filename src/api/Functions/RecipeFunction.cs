using System.Net;
using MenuCraft.Api.Dtos;
using MenuCraft.Api.Dtos.Recipes;
using MenuCraft.Api.Extensions;
using MenuCraft.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Functions;

public class RecipeFunction
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<RecipeFunction> _logger;

    public RecipeFunction(IRecipeService recipeService, ILogger<RecipeFunction> logger)
    {
        _recipeService = recipeService;
        _logger = logger;
    }

    [Function("GetRecipes")]
    public async Task<HttpResponseData> GetRecipesAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "recipes")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var familyGroupId = context.RequireFamilyGroupId();
        var recipes = await _recipeService.GetRecipesAsync(familyGroupId, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<IReadOnlyList<RecipeResponse>>.Ok(recipes), cancellationToken);
        return response;
    }

    [Function("GetRecipe")]
    public async Task<HttpResponseData> GetRecipeAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "recipes/{id:int}")] HttpRequestData req,
        int id,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var familyGroupId = context.RequireFamilyGroupId();
        var recipe = await _recipeService.GetRecipeAsync(id, familyGroupId, cancellationToken);

        if (recipe is null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(
                ApiResponse.Fail("レシピが見つかりません"), cancellationToken);
            return notFoundResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<RecipeResponse>.Ok(recipe), cancellationToken);
        return response;
    }

    [Function("CreateRecipe")]
    public async Task<HttpResponseData> CreateRecipeAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "recipes")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var familyGroupId = context.RequireFamilyGroupId();

        var request = await req.ReadFromJsonAsync<CreateRecipeRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.Title))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("タイトルは必須です"), cancellationToken);
            return badResponse;
        }

        var recipe = await _recipeService.CreateRecipeAsync(familyGroupId, request, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(
            ApiResponse<RecipeResponse>.Ok(recipe), cancellationToken);
        return response;
    }

    [Function("UpdateRecipe")]
    public async Task<HttpResponseData> UpdateRecipeAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "recipes/{id:int}")] HttpRequestData req,
        int id,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var familyGroupId = context.RequireFamilyGroupId();

        var request = await req.ReadFromJsonAsync<UpdateRecipeRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.Title))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("タイトルは必須です"), cancellationToken);
            return badResponse;
        }

        var recipe = await _recipeService.UpdateRecipeAsync(id, familyGroupId, request, cancellationToken);
        if (recipe is null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(
                ApiResponse.Fail("レシピが見つかりません"), cancellationToken);
            return notFoundResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<RecipeResponse>.Ok(recipe), cancellationToken);
        return response;
    }

    [Function("DeleteRecipe")]
    public async Task<HttpResponseData> DeleteRecipeAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "recipes/{id:int}")] HttpRequestData req,
        int id,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var familyGroupId = context.RequireFamilyGroupId();

        var deleted = await _recipeService.DeleteRecipeAsync(id, familyGroupId, cancellationToken);
        if (!deleted)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(
                ApiResponse.Fail("レシピが見つかりません"), cancellationToken);
            return notFoundResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.NoContent);
        return response;
    }
}
