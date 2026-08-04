using System.Text.Json;
using MenuCraft.Api.Data;
using MenuCraft.Api.Middleware;
using MenuCraft.Api.Models;
using MenuCraft.Api.Repositories;
using MenuCraft.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        var databaseProvider = DatabaseProviderResolver.Resolve(
            configuration[DatabaseProviderResolver.ConfigurationKey]);

        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                switch (databaseProvider)
                {
                    case DatabaseProvider.Sqlite:
                        options.UseSqlite(connectionString);
                        break;
                    default:
                        options.UseSqlServer(connectionString);
                        break;
                }
            });
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

        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IProfileService, ProfileService>();
    })
    .Build();

// DB スキーマの用意（SQLite のみ）とロール・初期管理者の冪等なシード
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var startupLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var startupConfiguration = services.GetRequiredService<IConfiguration>();
    var provider = DatabaseProviderResolver.Resolve(
        startupConfiguration[DatabaseProviderResolver.ConfigurationKey]);

    var dbContext = services.GetService<AppDbContext>();
    if (dbContext is not null)
    {
        await DatabaseInitializer.InitializeAsync(dbContext, provider, startupLogger);
    }

    var roleManager = services.GetService<RoleManager<IdentityRole<Guid>>>();
    if (roleManager is not null)
    {
        await RoleSeeder.SeedAsync(roleManager);
    }

    var userManager = services.GetService<UserManager<User>>();
    if (userManager is not null)
    {
        await AdminSeeder.SeedAsync(
            userManager,
            startupConfiguration[AdminSeeder.EmailConfigurationKey],
            startupConfiguration[AdminSeeder.PasswordConfigurationKey],
            startupLogger);
    }
}

host.Run();
