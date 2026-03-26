-- =============================================================================
-- MenuCraft Database Setup
-- Target: Azure SQL Database (SQL Server 12.0+)
-- Collation: SQL_Latin1_General_CP1_CI_AS
--
-- Usage:
--   Run this script on a fresh database to create all required tables.
--   Safe to re-run: each statement is guarded by IF NOT EXISTS checks.
--
-- Table creation order (dependency order):
--   1. FamilyGroups
--   2. ASP.NET Identity (AspNetRoles, AspNetUsers, ...)
--   3. Recipes, RecipeTags, RecipeIngredients
--   4. MealPlans, MealPlanRecipes
--   5. ShoppingListChecks
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. FamilyGroups
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FamilyGroups')
BEGIN
    CREATE TABLE FamilyGroups (
        Id          INT             NOT NULL IDENTITY(1,1),
        Name        NVARCHAR(100)   NOT NULL,
        InviteCode  NVARCHAR(20)    NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_FamilyGroups PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX UQ_FamilyGroups_InviteCode ON FamilyGroups (InviteCode);
END;
GO

-- -----------------------------------------------------------------------------
-- 2. ASP.NET Identity Tables
--    Schema matches IdentityDbContext<User, IdentityRole<Guid>, Guid>
-- -----------------------------------------------------------------------------

-- AspNetRoles
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetRoles')
BEGIN
    CREATE TABLE AspNetRoles (
        Id               UNIQUEIDENTIFIER NOT NULL,
        Name             NVARCHAR(256)    NULL,
        NormalizedName   NVARCHAR(256)    NULL,
        ConcurrencyStamp NVARCHAR(MAX)    NULL,

        CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX UQ_AspNetRoles_NormalizedName ON AspNetRoles (NormalizedName)
        WHERE NormalizedName IS NOT NULL;
END;
GO

-- AspNetUsers (User : IdentityUser<Guid> + FamilyGroupId)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    CREATE TABLE AspNetUsers (
        Id                   UNIQUEIDENTIFIER NOT NULL,
        FamilyGroupId        INT              NULL,
        CreatedAt            DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

        -- IdentityUser columns
        UserName             NVARCHAR(256)    NULL,
        NormalizedUserName   NVARCHAR(256)    NULL,
        Email                NVARCHAR(256)    NULL,
        NormalizedEmail      NVARCHAR(256)    NULL,
        EmailConfirmed       BIT              NOT NULL DEFAULT 0,
        PasswordHash         NVARCHAR(MAX)    NULL,
        SecurityStamp        NVARCHAR(MAX)    NULL,
        ConcurrencyStamp     NVARCHAR(MAX)    NULL,
        PhoneNumber          NVARCHAR(MAX)    NULL,
        PhoneNumberConfirmed BIT              NOT NULL DEFAULT 0,
        TwoFactorEnabled     BIT              NOT NULL DEFAULT 0,
        LockoutEnd           DATETIMEOFFSET   NULL,
        LockoutEnabled       BIT              NOT NULL DEFAULT 1,
        AccessFailedCount    INT              NOT NULL DEFAULT 0,

        CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id),
        CONSTRAINT FK_AspNetUsers_FamilyGroups FOREIGN KEY (FamilyGroupId)
            REFERENCES FamilyGroups (Id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX UQ_AspNetUsers_NormalizedUserName ON AspNetUsers (NormalizedUserName)
        WHERE NormalizedUserName IS NOT NULL;
    CREATE INDEX IX_AspNetUsers_NormalizedEmail ON AspNetUsers (NormalizedEmail);
    CREATE INDEX IX_AspNetUsers_FamilyGroupId ON AspNetUsers (FamilyGroupId);
END;
GO

-- AspNetUserClaims
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUserClaims')
BEGIN
    CREATE TABLE AspNetUserClaims (
        Id          INT              NOT NULL IDENTITY(1,1),
        UserId      UNIQUEIDENTIFIER NOT NULL,
        ClaimType   NVARCHAR(MAX)    NULL,
        ClaimValue  NVARCHAR(MAX)    NULL,

        CONSTRAINT PK_AspNetUserClaims PRIMARY KEY (Id),
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers FOREIGN KEY (UserId)
            REFERENCES AspNetUsers (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims (UserId);
END;
GO

-- AspNetUserLogins
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUserLogins')
BEGIN
    CREATE TABLE AspNetUserLogins (
        LoginProvider       NVARCHAR(128)    NOT NULL,
        ProviderKey         NVARCHAR(128)    NOT NULL,
        ProviderDisplayName NVARCHAR(MAX)    NULL,
        UserId              UNIQUEIDENTIFIER NOT NULL,

        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers FOREIGN KEY (UserId)
            REFERENCES AspNetUsers (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins (UserId);
END;
GO

-- AspNetUserTokens
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUserTokens')
BEGIN
    CREATE TABLE AspNetUserTokens (
        UserId        UNIQUEIDENTIFIER NOT NULL,
        LoginProvider NVARCHAR(128)    NOT NULL,
        Name          NVARCHAR(128)    NOT NULL,
        Value         NVARCHAR(MAX)    NULL,

        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers FOREIGN KEY (UserId)
            REFERENCES AspNetUsers (Id) ON DELETE CASCADE
    );
END;
GO

-- AspNetRoleClaims
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetRoleClaims')
BEGIN
    CREATE TABLE AspNetRoleClaims (
        Id         INT              NOT NULL IDENTITY(1,1),
        RoleId     UNIQUEIDENTIFIER NOT NULL,
        ClaimType  NVARCHAR(MAX)    NULL,
        ClaimValue NVARCHAR(MAX)    NULL,

        CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY (Id),
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles FOREIGN KEY (RoleId)
            REFERENCES AspNetRoles (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims (RoleId);
END;
GO

-- AspNetUserRoles
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUserRoles')
BEGIN
    CREATE TABLE AspNetUserRoles (
        UserId UNIQUEIDENTIFIER NOT NULL,
        RoleId UNIQUEIDENTIFIER NOT NULL,

        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers FOREIGN KEY (UserId)
            REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles FOREIGN KEY (RoleId)
            REFERENCES AspNetRoles (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles (RoleId);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Recipes
-- -----------------------------------------------------------------------------

-- Recipes
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Recipes')
BEGIN
    CREATE TABLE Recipes (
        Id            INT           NOT NULL IDENTITY(1,1),
        FamilyGroupId INT           NOT NULL,
        Title         NVARCHAR(200) NOT NULL,
        Url           NVARCHAR(2000) NULL,
        ImageUrl      NVARCHAR(2000) NULL,
        Description   NVARCHAR(4000) NULL,
        -- SourceType stored as string: 'Manual' | 'Web' | 'YouTube' | 'Instagram'
        SourceType    NVARCHAR(20)  NOT NULL DEFAULT 'Manual',
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Recipes PRIMARY KEY (Id),
        CONSTRAINT FK_Recipes_FamilyGroups FOREIGN KEY (FamilyGroupId)
            REFERENCES FamilyGroups (Id) ON DELETE CASCADE,
        CONSTRAINT CK_Recipes_SourceType CHECK (SourceType IN ('Manual', 'Web', 'YouTube', 'Instagram'))
    );

    CREATE INDEX IX_Recipes_FamilyGroupId ON Recipes (FamilyGroupId);
END;
GO

-- RecipeTags
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RecipeTags')
BEGIN
    CREATE TABLE RecipeTags (
        Id       INT          NOT NULL IDENTITY(1,1),
        RecipeId INT          NOT NULL,
        Name     NVARCHAR(50) NOT NULL,

        CONSTRAINT PK_RecipeTags PRIMARY KEY (Id),
        CONSTRAINT FK_RecipeTags_Recipes FOREIGN KEY (RecipeId)
            REFERENCES Recipes (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_RecipeTags_RecipeId ON RecipeTags (RecipeId);
END;
GO

-- RecipeIngredients
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RecipeIngredients')
BEGIN
    CREATE TABLE RecipeIngredients (
        Id       INT          NOT NULL IDENTITY(1,1),
        RecipeId INT          NOT NULL,
        Name     NVARCHAR(100) NOT NULL,
        Quantity NVARCHAR(50) NULL,
        Unit     NVARCHAR(30) NULL,

        CONSTRAINT PK_RecipeIngredients PRIMARY KEY (Id),
        CONSTRAINT FK_RecipeIngredients_Recipes FOREIGN KEY (RecipeId)
            REFERENCES Recipes (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_RecipeIngredients_RecipeId ON RecipeIngredients (RecipeId);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. MealPlans
-- -----------------------------------------------------------------------------

-- MealPlans
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MealPlans')
BEGIN
    CREATE TABLE MealPlans (
        Id            INT          NOT NULL IDENTITY(1,1),
        FamilyGroupId INT          NOT NULL,
        -- DateOnly stored as DATE in SQL Server
        Date          DATE         NOT NULL,
        -- MealType stored as string: 'Lunch' | 'Dinner'
        MealType      NVARCHAR(10) NOT NULL,
        CreatedAt     DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt     DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_MealPlans PRIMARY KEY (Id),
        CONSTRAINT FK_MealPlans_FamilyGroups FOREIGN KEY (FamilyGroupId)
            REFERENCES FamilyGroups (Id) ON DELETE CASCADE,
        CONSTRAINT CK_MealPlans_MealType CHECK (MealType IN ('Lunch', 'Dinner'))
    );

    -- Unique constraint: one meal plan per family per date per meal type
    CREATE UNIQUE INDEX UQ_MealPlans_GroupDateMealType
        ON MealPlans (FamilyGroupId, Date, MealType);
END;
GO

-- MealPlanRecipes
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MealPlanRecipes')
BEGIN
    CREATE TABLE MealPlanRecipes (
        Id         INT NOT NULL IDENTITY(1,1),
        MealPlanId INT NOT NULL,
        RecipeId   INT NOT NULL,

        CONSTRAINT PK_MealPlanRecipes PRIMARY KEY (Id),
        CONSTRAINT FK_MealPlanRecipes_MealPlans FOREIGN KEY (MealPlanId)
            REFERENCES MealPlans (Id) ON DELETE CASCADE,
        -- NO ACTION to avoid multiple cascade paths (Recipe already cascades via FamilyGroup)
        CONSTRAINT FK_MealPlanRecipes_Recipes FOREIGN KEY (RecipeId)
            REFERENCES Recipes (Id) ON DELETE NO ACTION
    );

    -- Unique constraint: a recipe appears only once per meal plan
    CREATE UNIQUE INDEX UQ_MealPlanRecipes_MealPlanRecipe
        ON MealPlanRecipes (MealPlanId, RecipeId);

    CREATE INDEX IX_MealPlanRecipes_RecipeId ON MealPlanRecipes (RecipeId);
END;
GO

-- -----------------------------------------------------------------------------
-- 5. ShoppingListChecks
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShoppingListChecks')
BEGIN
    CREATE TABLE ShoppingListChecks (
        Id             INT           NOT NULL IDENTITY(1,1),
        FamilyGroupId  INT           NOT NULL,
        WeekStartDate  DATE          NOT NULL,
        IngredientName NVARCHAR(100) NOT NULL,
        IsChecked      BIT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_ShoppingListChecks PRIMARY KEY (Id),
        CONSTRAINT FK_ShoppingListChecks_FamilyGroups FOREIGN KEY (FamilyGroupId)
            REFERENCES FamilyGroups (Id) ON DELETE CASCADE
    );

    -- Unique constraint: one check record per ingredient per week per family
    CREATE UNIQUE INDEX UQ_ShoppingListChecks_GroupWeekIngredient
        ON ShoppingListChecks (FamilyGroupId, WeekStartDate, IngredientName);
END;
GO

-- =============================================================================
-- Setup complete.
-- =============================================================================
