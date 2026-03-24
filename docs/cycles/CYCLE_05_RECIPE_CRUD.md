# Cycle 5: レシピ CRUD

## 目的
レシピの登録・一覧・編集・削除機能を実装する。

## タスクリスト
- [x] `IRecipeRepository` / `RecipeRepository`
  - `FindByIdAsync` — ID でレシピ検索（Tags, Ingredients 含む）
  - `GetByGroupIdAsync` — グループ別レシピ一覧（IsDeleted 除外）
  - `CreateAsync` — レシピ作成
  - `UpdateAsync` — レシピ更新（Tags, Ingredients の差し替え）
  - `DeleteAsync` — レシピ削除
- [x] `IRecipeService` / `RecipeService`
  - `GetRecipeAsync` — グループ ID 検証付き取得
  - `GetRecipesAsync` — グループ別一覧
  - `CreateRecipeAsync` — DTO → Entity 変換 + SourceType 自動判定
  - `UpdateRecipeAsync` — グループ ID 検証 + 更新
  - `DeleteRecipeAsync` — グループ ID 検証 + 削除
- [x] Recipe DTO
  - `CreateRecipeRequest`（Title, Url, ImageUrl, Description, Tags, Ingredients）
  - `UpdateRecipeRequest`（同上）
  - `RecipeResponse`（Id, Title, Url, ImageUrl, Description, SourceType, Tags, Ingredients, CreatedAt）
  - `IngredientRequest`（Name, Quantity, Unit）
  - `IngredientResponse`（Name, Quantity, Unit）
- [x] `RecipeFunction`
  - `GET /api/recipes` — 一覧
  - `GET /api/recipes/{id}` — 詳細
  - `POST /api/recipes` — 作成
  - `PUT /api/recipes/{id}` — 更新
  - `DELETE /api/recipes/{id}` — 削除
- [x] フロントエンド
  - `js/api/recipes.js` — API クライアント
  - `js/components/recipeCard.js` — レシピカードコンポーネント
  - `js/components/recipeForm.js` — レシピフォーム（モーダル）
  - `js/pages/recipes.js` — レシピ一覧ページ
- [x] 単体テスト（`RecipeServiceTests`）
  - 正常取得
  - 他グループアクセス拒否
  - 作成 + SourceType 自動判定
  - 削除 (正常 / 他グループ)
- [x] `Program.cs` に Recipe サービス登録

## 依存関係
- Cycle 4（グループ管理、認証済みコンテキスト）

## 設計判断
- SourceType 自動判定: URL に "youtube.com" / "youtu.be" → YouTube、"instagram.com" → Instagram、その他 → Web、URL なし → Manual
- レシピ更新時: Tags と Ingredients は全削除 → 再作成（差分更新ではなく置換）
- 削除は物理削除（IsDeleted フラグは将来の論理削除に備えた予約フィールド）
