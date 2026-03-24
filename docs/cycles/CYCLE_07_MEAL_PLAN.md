# Cycle 7: 献立ボード（CRUD + 表示）

## 目的
1週間の献立を手動で管理できるボード機能を実装する。

## タスクリスト
- [ ] `IMealPlanRepository` / `MealPlanRepository`
  - `GetWeeklyPlansAsync` — 指定週の全 MealPlan を取得（MealPlanRecipe + Recipe 含む）
  - `GetByDateAndTypeAsync` — 日付 + MealType で 1 件取得
  - `CreateOrUpdateAsync` — MealPlan 作成または更新
  - `ClearWeekAsync` — 指定週の全 MealPlan を削除
- [ ] `IMealPlanService` / `MealPlanService`
  - `GetWeeklyPlansAsync` — 週間献立取得（7日 × 2食のグリッドデータ）
  - `UpdateMealPlanAsync` — 1 セル分の献立更新（レシピ ID リスト）
  - 週開始日のバリデーション（月曜日であること）
- [ ] MealPlan DTO
  - `WeeklyMealPlanResponse`（WeekStart, Plans[]）
  - `MealPlanResponse`（Date, MealType, Recipes[]）
  - `UpdateMealPlanRequest`（RecipeIds[]）
- [ ] `MealPlanFunction`
  - `GET /api/mealplans?weekStart={date}` — 週間献立取得
  - `PUT /api/mealplans/{date}/{mealType}` — 1 食分更新
- [ ] フロントエンド
  - `js/api/mealplans.js` — API クライアント
  - `js/pages/mealPlanBoard.js` — 献立ボード（7日 × 2食グリッド）
  - 週切替ナビゲーション（前週 / 今週 / 翌週）
  - レシピ選択モーダル（登録済みレシピから選択）
  - レシピ解除ボタン
- [ ] 単体テスト
  - 週間献立取得
  - 1 セル更新
  - 週開始日バリデーション

## 依存関係
- Cycle 5（レシピ CRUD、RecipeResponse）

## 設計判断
- 週の開始日は月曜日で統一
- 1 セルに複数レシピを割り当て可能（MealPlanRecipe の 1:N リレーション）
- 更新は「全置換」方式（既存の MealPlanRecipe を削除して新規作成）
