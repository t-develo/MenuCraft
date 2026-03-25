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
    private readonly IOgpService _ogpService;
    private readonly IIngredientParserService _ingredientParserService;
    private readonly ILogger<RecipeFunction> _logger;

    public RecipeFunction(
        IRecipeService recipeService,
        IOgpService ogpService,
        IIngredientParserService ingredientParserService,
        ILogger<RecipeFunction> logger)
    {
        _recipeService = recipeService;
        _ogpService = ogpService;
        _ingredientParserService = ingredientParserService;
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

    [Function("FetchOgp")]
    public async Task<HttpResponseData> FetchOgpAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "recipes/fetch-ogp")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        // Auth check — must belong to a family group
        context.RequireFamilyGroupId();

        var request = await req.ReadFromJsonAsync<FetchOgpRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.Url))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("URLは必須です"), cancellationToken);
            return badResponse;
        }

        var ogp = await _ogpService.FetchOgpAsync(request.Url, cancellationToken);
        if (ogp is null)
        {
            var failResponse = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
            await failResponse.WriteAsJsonAsync(
                ApiResponse.Fail("OGP情報の取得に失敗しました"), cancellationToken);
            return failResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<OgpResponse>.Ok(ogp), cancellationToken);
        return response;
    }

    [Function("ParseIngredients")]
    public async Task<HttpResponseData> ParseIngredientsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "recipes/parse-ingredients")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        // Auth check — must belong to a family group
        context.RequireFamilyGroupId();

        var request = await req.ReadFromJsonAsync<ParseIngredientsRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.Text))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("テキストは必須です"), cancellationToken);
            return badResponse;
        }

        var result = _ingredientParserService.ParseIngredients(request.Text);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<ParseIngredientsResponse>.Ok(result), cancellationToken);
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
