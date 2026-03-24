# Cycle 2: データモデル & EF Core エンティティ

## 目的
アプリケーションの全エンティティを定義し、EF Core でリレーションシップ・制約を構成する。

## 成果物
- [x] エンティティモデル
  - `User`（IdentityUser 継承、FamilyGroupId 追加）
  - `FamilyGroup`（Name, InviteCode）
  - `Recipe`（Title, Url, ImageUrl, Description, SourceType, IsDeleted）
  - `RecipeTag`（Name）
  - `RecipeIngredient`（Name, Quantity, Unit）
  - `MealPlan`（Date, MealType）
  - `MealPlanRecipe`（MealPlanId, RecipeId）
  - `ShoppingListCheck`（WeekStartDate, IngredientName, IsChecked）
- [x] 列挙型
  - `MealType`（Lunch, Dinner）
  - `SourceType`（Manual, Web, YouTube, Instagram）
- [x] `AppDbContext` 拡張
  - IdentityDbContext 継承
  - DbSet 定義
  - リレーション設定（1:N, N:1）
  - ユニーク制約
  - カスケード削除ルール
  - 文字列最大長制約
- [x] `Program.cs` に Identity 統合
- [x] 統合テスト（`AppDbContextTests`）
  - CRUD 操作
  - カスケード削除検証

## 依存関係
- Cycle 1（プロジェクト構造）

## 設計判断
- `SourceType` は `string` 変換で保存（拡張性のため）
- `MealType` も `string` 変換
- `User.FamilyGroupId` は nullable（グループ未所属の状態をサポート）
- 削除時: FamilyGroup → User は SET NULL、その他は CASCADE
