using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace MenuCraft.Api.Middleware;

/// <summary>
/// Adds security headers to all API responses.
/// Replaces the globalHeaders previously set by staticwebapp.config.json.
/// </summary>
public class SecurityHeadersMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        await next(context);

        var requestData = await context.GetHttpRequestDataAsync();
        if (requestData is null)
        {
            return;
        }

        var response = context.GetInvocationResult().Value as HttpResponseData;
        if (response is null)
        {
            return;
        }

        response.Headers.TryAddWithoutValidation("X-Robots-Tag", "noindex, nofollow");
        response.Headers.TryAddWithoutValidation("X-Content-Type-Options", "nosniff");
        response.Headers.TryAddWithoutValidation("X-Frame-Options", "DENY");
        response.Headers.TryAddWithoutValidation("Referrer-Policy", "strict-origin-when-cross-origin");
    }
}
