using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MenuCraft.Api.Middleware;

public class JwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    private static readonly HashSet<string> AnonymousRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Health",
        "AuthRegister",
        "AuthLogin",
        "AuthRefresh"
    };

    public JwtAuthenticationMiddleware(
        IConfiguration configuration,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var functionName = context.FunctionDefinition.Name;

        if (AnonymousRoutes.Contains(functionName))
        {
            await next(context);
            return;
        }

        var requestData = await context.GetHttpRequestDataAsync();
        if (requestData is null)
        {
            await next(context);
            return;
        }

        // CORS preflight requests must not require authentication
        if (requestData.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!requestData.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            var response = requestData.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
            await response.WriteAsJsonAsync(new { success = false, error = "Authorization header required" });
            context.GetInvocationResult().Value = response;
            return;
        }

        var authHeader = authHeaders.FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var response = requestData.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
            await response.WriteAsJsonAsync(new { success = false, error = "Bearer token required" });
            context.GetInvocationResult().Value = response;
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var principal = ValidateToken(token);
            context.Items["User"] = principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid JWT token");
            var response = requestData.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
            await response.WriteAsJsonAsync(new { success = false, error = "Invalid or expired token" });
            context.GetInvocationResult().Value = response;
            return;
        }

        await next(context);
    }

    private ClaimsPrincipal ValidateToken(string token)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured.");
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT issuer not configured.");
        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT audience not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, validationParameters, out _);
    }
}
