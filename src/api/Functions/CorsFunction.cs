using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace MenuCraft.Api.Functions;

/// <summary>
/// Catch-all handler for CORS preflight (OPTIONS) requests.
/// Returns 204 No Content; CorsMiddleware adds the necessary CORS response headers.
/// This ensures OPTIONS requests reach the worker process so the middleware can act on them.
/// </summary>
public class CorsFunction
{
    [Function("CorsPreflight")]
    public HttpResponseData HandlePreflight(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "{*rest}")] HttpRequestData req)
    {
        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}
