using System.Text.Json;
using MenuCraft.Api.Data;
using MenuCraft.Api.Middleware;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MenuCraft.Api.Data;
using Microsoft.AspNetCore.Identity;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.UseMiddleware<CorsMiddleware>();
        builder.UseMiddleware<SecurityHeadersMiddleware>();
        builder.UseMiddleware<JwtAuthenticationMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorkerOptions>(workerOptions =>
        {
            workerOptions.Serializer = new Azure.Core.Serialization.JsonObjectSerializer(
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });
        });
        var configuration = context.Configuration;

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var connectionString = configuration["ConnectionStrings:Default"];
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IGroupService, GroupService>();

        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeService, RecipeService>();

        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IMealPlanService, MealPlanService>();

        services.AddScoped<IShoppingRepository, ShoppingRepository>();
        services.AddScoped<IShoppingService, ShoppingService>();

        services.AddHttpClient("OgpClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "MenuCraft/1.0 (OGP Fetcher)");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            MaxAutomaticRedirections = 5,
            AllowAutoRedirect = true
        });

        services.AddSingleton<IOgpService, OgpService>();
        services.AddSingleton<IIngredientParserService, IngredientParserService>();
    })
    .Build();

// ロールを冪等に作成
using (var scope = host.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole<Guid>>>();
    if (roleManager is not null)
    {
        await RoleSeeder.SeedAsync(roleManager);
    }
}

host.Run();
