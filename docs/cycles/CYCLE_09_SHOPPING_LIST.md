# Cycle 9: 買い物リスト

## 目的
献立に基づいて必要な材料を自動集約し、チェックリスト付きの買い物リストを生成する。

## タスクリスト
- [ ] `IShoppingService` / `ShoppingService`
  - `GetShoppingListAsync` — 指定週の献立から材料を集約
    - 全 MealPlan → MealPlanRecipe → Recipe → RecipeIngredients を展開
    - 材料名でグルーピング
    - 同一材料・同一単位の数量を合算（数値の場合）
    - ShoppingListCheck からチェック状態を取得してマージ
  - `UpdateCheckAsync` — チェック状態の更新
    - ShoppingListCheck を upsert（存在すれば更新、なければ作成）
- [ ] Shopping DTO
  - `ShoppingListResponse`（WeekStart, Items[]）
  - `ShoppingItemResponse`（IngredientName, TotalQuantity, Unit, IsChecked, Sources[]）
  - `ShoppingItemSource`（RecipeName, Quantity, Unit）
  - `UpdateCheckRequest`（WeekStart, IngredientName, IsChecked）
- [ ] `ShoppingFunction`
  - `GET /api/shopping-list?weekStart={date}` — 買い物リスト取得
  - `PUT /api/shopping-list/check` — チェック状態更新
- [ ] フロントエンド
  - `js/api/shopping.js` — API クライアント
  - `js/pages/shoppingList.js` — 買い物リストページ
    - 材料一覧表示（チェックボックス付き）
    - チェック時に API 呼び出し
    - 「どのレシピで使うか」の表示
    - 週切替ナビゲーション
- [ ] 単体テスト
  - 材料集約（同一材料の合算）
  - チェック状態の永続化
  - 献立なし時の空リスト
  - 数量の合算ロジック

## 依存関係
- Cycle 7/8（献立データ）

## 集約アルゴリズム詳細

```
Input:  weekStart, familyGroupId
Output: ShoppingListResponse

1. 指定週の MealPlans を取得（MealPlanRecipe → Recipe → RecipeIngredients 含む）
2. 全 RecipeIngredients を展開
3. 材料名（正規化後）でグルーピング
4. 各グループ:
   a. 単位が同一で数量が数値 → 合算
   b. 単位が異なるまたは数量が非数値 → 列挙
   c. ソースレシピ名を記録
5. ShoppingListCheck テーブルからチェック状態を取得
6. マージして返却
```

## 設計判断
- 数量の合算は「同一単位 + 数値」の場合のみ（MVP）
- 単位正規化（g/グラム 統一等）は Phase 3 で対応
- チェック状態は (FamilyGroupId, WeekStartDate, IngredientName) で管理
