using MenuCraft.Api.Models;
using MenuCraft.Api.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MenuCraft.Api.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<FamilyGroup> FamilyGroups => Set<FamilyGroup>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeTag> RecipeTags => Set<RecipeTag>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<MealPlanRecipe> MealPlanRecipes => Set<MealPlanRecipe>();
    public DbSet<ShoppingListCheck> ShoppingListChecks => Set<ShoppingListCheck>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(u => u.FamilyGroup)
                .WithMany(g => g.Members)
                .HasForeignKey(u => u.FamilyGroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // FamilyGroup
        modelBuilder.Entity<FamilyGroup>(entity =>
        {
            entity.HasIndex(g => g.InviteCode).IsUnique();
            entity.Property(g => g.Name).HasMaxLength(100).IsRequired();
            entity.Property(g => g.InviteCode).HasMaxLength(20).IsRequired();
        });

        // Recipe
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.Property(r => r.Title).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Url).HasMaxLength(2000);
            entity.Property(r => r.ImageUrl).HasMaxLength(2000);
            entity.Property(r => r.Description).HasMaxLength(4000);
            entity.Property(r => r.SourceType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(r => r.FamilyGroup)
                .WithMany(g => g.Recipes)
                .HasForeignKey(r => r.FamilyGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.FamilyGroupId);
        });

        // RecipeTag
        modelBuilder.Entity<RecipeTag>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(50).IsRequired();

            entity.HasOne(t => t.Recipe)
                .WithMany(r => r.Tags)
                .HasForeignKey(t => t.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RecipeIngredient
        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.Property(i => i.Name).HasMaxLength(100).IsRequired();
            entity.Property(i => i.Quantity).HasMaxLength(50);
            entity.Property(i => i.Unit).HasMaxLength(30);

            entity.HasOne(i => i.Recipe)
                .WithMany(r => r.Ingredients)
                .HasForeignKey(i => i.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MealPlan
        modelBuilder.Entity<MealPlan>(entity =>
        {
            entity.Property(m => m.MealType)
                .HasConversion<string>()
                .HasMaxLength(10);

            entity.HasOne(m => m.FamilyGroup)
                .WithMany(g => g.MealPlans)
                .HasForeignKey(m => m.FamilyGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => new { m.FamilyGroupId, m.Date, m.MealType })
                .IsUnique();
        });

        // MealPlanRecipe
        modelBuilder.Entity<MealPlanRecipe>(entity =>
        {
            entity.HasOne(mpr => mpr.MealPlan)
                .WithMany(m => m.MealPlanRecipes)
                .HasForeignKey(mpr => mpr.MealPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mpr => mpr.Recipe)
                .WithMany(r => r.MealPlanRecipes)
                .HasForeignKey(mpr => mpr.RecipeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(mpr => new { mpr.MealPlanId, mpr.RecipeId })
                .IsUnique();
        });

        // ShoppingListCheck
        modelBuilder.Entity<ShoppingListCheck>(entity =>
        {
            entity.Property(s => s.IngredientName).HasMaxLength(100).IsRequired();

            entity.HasOne(s => s.FamilyGroup)
                .WithMany(g => g.ShoppingListChecks)
                .HasForeignKey(s => s.FamilyGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.FamilyGroupId, s.WeekStartDate, s.IngredientName })
                .IsUnique();
        });
    }
}
