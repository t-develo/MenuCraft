using MenuCraft.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Data;

/// <summary>
/// Optionally creates an initial administrator account from configuration.
///
/// This exists as a provider-agnostic replacement for the hard-coded PBKDF2 hash in
/// <c>infra/sql/setup.sql</c>, which cannot run against SQLite.
///
/// It is opt-in: both <c>Seed:AdminEmail</c> and <c>Seed:AdminPassword</c> must be set.
/// When they are not, nothing happens — a fresh install instead relies on
/// <c>AuthFunction.RegisterAsync</c>, which grants the Admin role to the first user who
/// registers. Use this seeder to provision a known admin up front, or to regain
/// administrator access on an existing installation.
/// </summary>
public static class AdminSeeder
{
    public const string EmailConfigurationKey = "Seed:AdminEmail";
    public const string PasswordConfigurationKey = "Seed:AdminPassword";

    private const string AdminRole = "Admin";

    public static async Task SeedAsync(
        UserManager<User> userManager,
        string? adminEmail,
        string? adminPassword,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogInformation(
                "{EmailKey} / {PasswordKey} が未設定のため、初期管理者のシードをスキップします",
                EmailConfigurationKey, PasswordConfigurationKey);
            return;
        }

        var email = adminEmail.Trim();

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Idempotent: an existing account keeps its current password. Only make sure it
            // actually holds the Admin role, so a half-provisioned install can be repaired.
            if (!await userManager.IsInRoleAsync(existing, AdminRole))
            {
                var roleResult = await userManager.AddToRoleAsync(existing, AdminRole);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"既存ユーザー '{email}' への Admin ロール付与に失敗しました: {Describe(roleResult)}");
                }

                logger.LogInformation("Granted the Admin role to the existing seeded account");
            }

            logger.LogInformation("初期管理者は既に存在するため、シードをスキップします");
            return;
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, adminPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"初期管理者アカウントの作成に失敗しました: {Describe(createResult)}");
        }

        var addToRoleResult = await userManager.AddToRoleAsync(user, AdminRole);
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"初期管理者への Admin ロール付与に失敗しました: {Describe(addToRoleResult)}");
        }

        // The email is safe to log; the password never is.
        logger.LogInformation("Seeded the initial administrator account {Email}", email);
    }

    private static string Describe(IdentityResult result) =>
        string.Join(", ", result.Errors.Select(e => e.Description));
}
