using System.Net;
using MenuCraft.Api.Dtos;
using MenuCraft.Api.Dtos.MealPlans;
using MenuCraft.Api.Extensions;
using MenuCraft.Api.Models.Enums;
using MenuCraft.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Functions;

public class MealPlanFunction
{
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<MealPlanFunction> _logger;

    public MealPlanFunction(IMealPlanService mealPlanService, ILogger<MealPlanFunction> logger)
    {
        _mealPlanService = mealPlanService;
        _logger = logger;
    }

    [Function("GetWeeklyMealPlan")]
    public async Task<HttpResponseData> GetWeeklyMealPlanAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mealplans")] HttpRequestData req,
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

        try
        {
            var result = await _mealPlanService.GetWeeklyPlansAsync(familyGroupId, weekStart, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(
                ApiResponse<WeeklyMealPlanResponse>.Ok(result), cancellationToken);
            return response;
        }
        catch (ArgumentException ex)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail(ex.Message), cancellationToken);
            return badResponse;
        }
    }

    [Function("UpdateMealPlan")]
    public async Task<HttpResponseData> UpdateMealPlanAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "mealplans/{date}/{mealType}")] HttpRequestData req,
        string date,
        string mealType,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var familyGroupId = context.RequireFamilyGroupId();

        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("日付の形式が正しくありません (例: 2024-01-15)"), cancellationToken);
            return badResponse;
        }

        if (!Enum.TryParse<MealType>(mealType, ignoreCase: true, out var parsedMealType))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("mealType は 'Lunch' または 'Dinner' である必要があります"), cancellationToken);
            return badResponse;
        }

        var request = await req.ReadFromJsonAsync<UpdateMealPlanRequest>(cancellationToken);
        if (request is null)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                ApiResponse.Fail("リクエストボディが不正です"), cancellationToken);
            return badResponse;
        }

        var result = await _mealPlanService.UpdateMealPlanAsync(
            familyGroupId, parsedDate, parsedMealType, request, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<MealPlanResponse>.Ok(result), cancellationToken);
        return response;
    }
}
