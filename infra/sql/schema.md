# MenuCraft — Database Schema

## ER Diagram

```
FamilyGroups
  │
  ├──< AspNetUsers (FamilyGroupId, SET NULL)
  │
  ├──< Recipes (FamilyGroupId, CASCADE)
  │     ├──< RecipeTags    (RecipeId, CASCADE)
  │     ├──< RecipeIngredients (RecipeId, CASCADE)
  │     └──< MealPlanRecipes  (RecipeId, NO ACTION)
  │
  ├──< MealPlans (FamilyGroupId, CASCADE)
  │     └──< MealPlanRecipes (MealPlanId, CASCADE)
  │
  └──< ShoppingListChecks (FamilyGroupId, CASCADE)

AspNetUsers
  ├──< AspNetUserClaims  (UserId, CASCADE)
  ├──< AspNetUserLogins  (UserId, CASCADE)
  ├──< AspNetUserTokens  (UserId, CASCADE)
  ├──< AspNetUserRoles   (UserId, CASCADE)
  └──< RefreshTokens     (UserId, CASCADE)

AspNetRoles
  ├──< AspNetRoleClaims  (RoleId, CASCADE)
  └──< AspNetUserRoles   (RoleId, CASCADE)
```

---

## Tables

### FamilyGroups

| Column     | Type          | Nullable | Default           | Notes                 |
|------------|---------------|----------|-------------------|-----------------------|
| Id         | INT           | NO       | IDENTITY(1,1)     | PK                    |
| Name       | NVARCHAR(100) | NO       |                   |                       |
| InviteCode | NVARCHAR(20)  | NO       |                   | UNIQUE                |
| CreatedAt  | DATETIME2     | NO       | SYSUTCDATETIME()  |                       |

---

### AspNetUsers

| Column              | Type             | Nullable | Default          | Notes                         |
|---------------------|------------------|----------|------------------|-------------------------------|
| Id                  | UNIQUEIDENTIFIER | NO       |                  | PK                            |
| FamilyGroupId       | INT              | YES      |                  | FK → FamilyGroups, SET NULL   |
| CreatedAt           | DATETIME2        | NO       | SYSUTCDATETIME() |                               |
| UserName            | NVARCHAR(256)    | YES      |                  | UNIQUE (NormalizedUserName)   |
| NormalizedUserName  | NVARCHAR(256)    | YES      |                  |                               |
| Email               | NVARCHAR(256)    | YES      |                  |                               |
| NormalizedEmail     | NVARCHAR(256)    | YES      |                  | Index                         |
| EmailConfirmed      | BIT              | NO       | 0                |                               |
| PasswordHash        | NVARCHAR(MAX)    | YES      |                  |                               |
| SecurityStamp       | NVARCHAR(MAX)    | YES      |                  |                               |
| ConcurrencyStamp    | NVARCHAR(MAX)    | YES      |                  |                               |
| PhoneNumber         | NVARCHAR(MAX)    | YES      |                  |                               |
| PhoneNumberConfirmed| BIT              | NO       | 0                |                               |
| TwoFactorEnabled    | BIT              | NO       | 0                |                               |
| LockoutEnd          | DATETIMEOFFSET   | YES      |                  |                               |
| LockoutEnabled      | BIT              | NO       | 1                |                               |
| AccessFailedCount   | INT              | NO       | 0                |                               |

---

### AspNetRoles

| Column          | Type             | Nullable | Notes  |
|-----------------|------------------|----------|--------|
| Id              | UNIQUEIDENTIFIER | NO       | PK     |
| Name            | NVARCHAR(256)    | YES      |        |
| NormalizedName  | NVARCHAR(256)    | YES      | UNIQUE |
| ConcurrencyStamp| NVARCHAR(MAX)    | YES      |        |

---

### AspNetUserClaims

| Column     | Type             | Nullable | Notes                      |
|------------|------------------|----------|----------------------------|
| Id         | INT              | NO       | PK, IDENTITY               |
| UserId     | UNIQUEIDENTIFIER | NO       | FK → AspNetUsers, CASCADE  |
| ClaimType  | NVARCHAR(MAX)    | YES      |                            |
| ClaimValue | NVARCHAR(MAX)    | YES      |                            |

---

### AspNetUserLogins

| Column             | Type             | Nullable | Notes                      |
|--------------------|------------------|----------|----------------------------|
| LoginProvider      | NVARCHAR(128)    | NO       | PK (composite)             |
| ProviderKey        | NVARCHAR(128)    | NO       | PK (composite)             |
| ProviderDisplayName| NVARCHAR(MAX)    | YES      |                            |
| UserId             | UNIQUEIDENTIFIER | NO       | FK → AspNetUsers, CASCADE  |

---

### AspNetUserTokens

| Column        | Type             | Nullable | Notes                      |
|---------------|------------------|----------|----------------------------|
| UserId        | UNIQUEIDENTIFIER | NO       | PK (composite)             |
| LoginProvider | NVARCHAR(128)    | NO       | PK (composite)             |
| Name          | NVARCHAR(128)    | NO       | PK (composite)             |
| Value         | NVARCHAR(MAX)    | YES      |                            |

---

### AspNetRoleClaims

| Column     | Type             | Nullable | Notes                      |
|------------|------------------|----------|----------------------------|
| Id         | INT              | NO       | PK, IDENTITY               |
| RoleId     | UNIQUEIDENTIFIER | NO       | FK → AspNetRoles, CASCADE  |
| ClaimType  | NVARCHAR(MAX)    | YES      |                            |
| ClaimValue | NVARCHAR(MAX)    | YES      |                            |

---

### AspNetUserRoles

| Column | Type             | Nullable | Notes                      |
|--------|------------------|----------|----------------------------|
| UserId | UNIQUEIDENTIFIER | NO       | PK (composite)             |
| RoleId | UNIQUEIDENTIFIER | NO       | PK (composite)             |
|        |                  |          | FK → AspNetUsers, CASCADE  |
|        |                  |          | FK → AspNetRoles, CASCADE  |

---

### Recipes

| Column       | Type          | Nullable | Default          | Notes                              |
|--------------|---------------|----------|------------------|------------------------------------|
| Id           | INT           | NO       | IDENTITY(1,1)    | PK                                 |
| FamilyGroupId| INT           | NO       |                  | FK → FamilyGroups, CASCADE; Index  |
| Title        | NVARCHAR(200) | NO       |                  |                                    |
| Url          | NVARCHAR(2000)| YES      |                  |                                    |
| ImageUrl     | NVARCHAR(2000)| YES      |                  |                                    |
| Description  | NVARCHAR(4000)| YES      |                  |                                    |
| SourceType   | NVARCHAR(20)  | NO       | 'Manual'         | CHECK: Manual/Web/YouTube/Instagram|
| IsDeleted    | BIT           | NO       | 0                | 論理削除フラグ                      |
| CreatedAt    | DATETIME2     | NO       | SYSUTCDATETIME() |                                    |
| UpdatedAt    | DATETIME2     | NO       | SYSUTCDATETIME() |                                    |

---

### RecipeTags

| Column   | Type         | Nullable | Notes                    |
|----------|--------------|----------|--------------------------|
| Id       | INT          | NO       | PK, IDENTITY             |
| RecipeId | INT          | NO       | FK → Recipes, CASCADE    |
| Name     | NVARCHAR(50) | NO       |                          |

---

### RecipeIngredients

| Column   | Type          | Nullable | Notes                    |
|----------|---------------|----------|--------------------------|
| Id       | INT           | NO       | PK, IDENTITY             |
| RecipeId | INT           | NO       | FK → Recipes, CASCADE    |
| Name     | NVARCHAR(100) | NO       |                          |
| Quantity | NVARCHAR(50)  | YES      |                          |
| Unit     | NVARCHAR(30)  | YES      |                          |

---

### MealPlans

| Column       | Type         | Nullable | Default          | Notes                                       |
|--------------|--------------|----------|------------------|---------------------------------------------|
| Id           | INT          | NO       | IDENTITY(1,1)    | PK                                          |
| FamilyGroupId| INT          | NO       |                  | FK → FamilyGroups, CASCADE                  |
| Date         | DATE         | NO       |                  | C# DateOnly にマッピング                     |
| MealType     | NVARCHAR(10) | NO       |                  | CHECK: Lunch/Dinner                         |
| CreatedAt    | DATETIME2    | NO       | SYSUTCDATETIME() |                                             |
| UpdatedAt    | DATETIME2    | NO       | SYSUTCDATETIME() |                                             |

**Unique index:** `(FamilyGroupId, Date, MealType)`

---

### MealPlanRecipes

| Column    | Type | Nullable | Notes                         |
|-----------|------|----------|-------------------------------|
| Id        | INT  | NO       | PK, IDENTITY                  |
| MealPlanId| INT  | NO       | FK → MealPlans, CASCADE       |
| RecipeId  | INT  | NO       | FK → Recipes, NO ACTION       |

**Unique index:** `(MealPlanId, RecipeId)`

> `RecipeId` の FK を NO ACTION にした理由: FamilyGroup → Recipe と FamilyGroup → MealPlan → MealPlanRecipe の二重カスケードパスを避けるため（EF Core の `DeleteBehavior.NoAction` と一致）。

---

### ShoppingListChecks

| Column        | Type          | Nullable | Default | Notes                                |
|---------------|---------------|----------|---------|--------------------------------------|
| Id            | INT           | NO       | IDENTITY(1,1) | PK                            |
| FamilyGroupId | INT           | NO       |         | FK → FamilyGroups, CASCADE           |
| WeekStartDate | DATE          | NO       |         | 週の開始日（月曜日）                  |
| IngredientName| NVARCHAR(100) | NO       |         |                                      |
| IsChecked     | BIT           | NO       | 0       |                                      |

**Unique index:** `(FamilyGroupId, WeekStartDate, IngredientName)`

---

### RefreshTokens

| Column     | Type             | Nullable | Default          | Notes                     |
|------------|------------------|----------|------------------|---------------------------|
| Id         | UNIQUEIDENTIFIER | NO       | NEWID()          | PK                        |
| UserId     | UNIQUEIDENTIFIER | NO       |                  | FK → AspNetUsers, CASCADE |
| Token      | NVARCHAR(128)    | NO       |                  | UNIQUE                    |
| ExpiresAt  | DATETIME2        | NO       |                  |                           |
| CreatedAt  | DATETIME2        | NO       | SYSUTCDATETIME() |                           |
| IsRevoked  | BIT              | NO       | 0                |                           |
| DeviceInfo | NVARCHAR(MAX)    | YES      |                  |                           |

---

## Indexes Summary

| Table              | Index Name                                | Columns                              | Type   |
|--------------------|-------------------------------------------|--------------------------------------|--------|
| FamilyGroups       | UQ_FamilyGroups_InviteCode                | InviteCode                           | UNIQUE |
| AspNetUsers        | UQ_AspNetUsers_NormalizedUserName         | NormalizedUserName                   | UNIQUE |
| AspNetUsers        | IX_AspNetUsers_NormalizedEmail            | NormalizedEmail                      |        |
| AspNetUsers        | IX_AspNetUsers_FamilyGroupId              | FamilyGroupId                        |        |
| AspNetRoles        | UQ_AspNetRoles_NormalizedName             | NormalizedName                       | UNIQUE |
| AspNetUserClaims   | IX_AspNetUserClaims_UserId                | UserId                               |        |
| AspNetUserLogins   | IX_AspNetUserLogins_UserId                | UserId                               |        |
| AspNetRoleClaims   | IX_AspNetRoleClaims_RoleId                | RoleId                               |        |
| AspNetUserRoles    | IX_AspNetUserRoles_RoleId                 | RoleId                               |        |
| Recipes            | IX_Recipes_FamilyGroupId                  | FamilyGroupId                        |        |
| RecipeTags         | IX_RecipeTags_RecipeId                    | RecipeId                             |        |
| RecipeIngredients  | IX_RecipeIngredients_RecipeId             | RecipeId                             |        |
| MealPlans          | UQ_MealPlans_GroupDateMealType            | FamilyGroupId, Date, MealType        | UNIQUE |
| MealPlanRecipes    | UQ_MealPlanRecipes_MealPlanRecipe         | MealPlanId, RecipeId                 | UNIQUE |
| MealPlanRecipes    | IX_MealPlanRecipes_RecipeId               | RecipeId                             |        |
| ShoppingListChecks | UQ_ShoppingListChecks_GroupWeekIngredient | FamilyGroupId, WeekStartDate, IngredientName | UNIQUE |
| RefreshTokens      | UQ_RefreshTokens_Token                   | Token                                        | UNIQUE |
| RefreshTokens      | IX_RefreshTokens_UserId                  | UserId                                       |        |
