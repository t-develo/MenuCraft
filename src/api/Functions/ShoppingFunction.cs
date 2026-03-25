using System.Net;
using MenuCraft.Api.Dtos;
using MenuCraft.Api.Dtos.Shopping;
using MenuCraft.Api.Extensions;
using MenuCraft.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Functions;

public class ShoppingFunction
{
    private readonly IShoppingService _shoppingService;
    private readonly ILogger<ShoppingFunction> _logger;

    public ShoppingFunction(IShoppingService shoppingService, ILogger<ShoppingFunction> logger)
    {
        _shoppingService = shoppingService;
        _logger = logger;
    }

    [Function("GetShoppingList")]
    public async Task<HttpResponseData> GetShoppingListAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shopping-list")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var familyGroupId = context.RequireFamilyGroupId();

        var weekStartStr = req.Query["weekStart"];
        if (string.IsNullOrWhiteSpace(weekStartStr) || !DateOnly.TryParse(weekStartStr, out var weekStart))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("weekStart パラメーターは必須です (例: 2024-01-15)"), cancellationToken);
            return badResponse;
        }

        var result = await _shoppingService.GetShoppingListAsync(familyGroupId, weekStart, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<ShoppingListResponse>.Ok(result), cancellationToken);
        return response;
    }

    [Function("UpdateShoppingCheck")]
    public async Task<HttpResponseData> UpdateShoppingCheckAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "shopping-list/check")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var familyGroupId = context.RequireFamilyGroupId();

        var request = await req.ReadFromJsonAsync<UpdateCheckRequest>(cancellationToken);
        if (request is null)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("リクエストボディが不正です"), cancellationToken);
            return badResponse;
        }

        try
        {
            await _shoppingService.UpdateCheckAsync(familyGroupId, request, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse.Ok(), cancellationToken);
            return response;
        }
        catch (ArgumentException ex)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ApiResponse.Fail(ex.Message), cancellationToken);
            return badResponse;
        }
    }
}
