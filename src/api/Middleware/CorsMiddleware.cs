using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;

namespace MenuCraft.Api.Middleware;

/// <summary>
/// Adds CORS headers to all HTTP responses.
/// Works in tandem with the catch-all CorsPreflight function to handle OPTIONS preflight requests.
/// Platform-level CORS in Azure Functions (siteConfig.cors) is the primary mechanism;
/// this middleware provides belt-and-suspenders coverage.
/// </summary>
public class CorsMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IReadOnlyList<string> _allowedOrigins;

    public CorsMiddleware(IConfiguration configuration)
    {
        _allowedOrigins = configuration["AllowedOrigins"]
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        await next(context);

        var request = await context.GetHttpRequestDataAsync();
        if (request is null)
        {
            return;
        }

        var response = context.GetInvocationResult().Value as HttpResponseData;
        if (response is null)
        {
            return;
        }

        if (!request.Headers.TryGetValues("Origin", out var originValues))
        {
            return;
        }

        var origin = originValues.FirstOrDefault();
        if (origin is null)
        {
            return;
        }

        var isAllowed = _allowedOrigins.Any(o =>
            o.Equals("*", StringComparison.Ordinal) ||
            o.Equals(origin, StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            return;
        }

        response.Headers.TryAddWithoutValidation("Access-Control-Allow-Origin", origin);
        response.Headers.TryAddWithoutValidation("Vary", "Origin");

        if (request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            var requestedHeaders = request.Headers.TryGetValues("Access-Control-Request-Headers", out var headerValues)
                ? headerValues.FirstOrDefault()
                : null;

            response.Headers.TryAddWithoutValidation("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.TryAddWithoutValidation("Access-Control-Allow-Headers",
                string.IsNullOrEmpty(requestedHeaders) ? "Content-Type, Authorization" : requestedHeaders);
            response.Headers.TryAddWithoutValidation("Access-Control-Max-Age", "86400");
        }
    }
}
