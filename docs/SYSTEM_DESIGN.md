# MenuCraft — システム設計書

## 1. データベース設計

### ER 図

```
┌─────────────────┐     ┌─────────────────────┐
│ AspNetUsers      │     │ FamilyGroups        │
│ (User extends)   │     │                     │
├─────────────────┤     ├─────────────────────┤
│ Id (GUID) PK    │────►│ Id (int) PK         │
│ Email           │  N:1│ Name (100)          │
│ FamilyGroupId FK│     │ InviteCode (20) UQ  │
│ CreatedAt       │     │ CreatedAt           │
│ PasswordHash    │     └──────┬──────────────┘
│ ...Identity cols│            │ 1:N
└─────────────────┘            │
                               ▼
                    ┌─────────────────────┐
                    │ Recipes             │
                    ├─────────────────────┤
                    │ Id (int) PK         │
                    │ FamilyGroupId FK    │
                    │ Title (200)         │
                    │ Url (2000)          │
                    │ ImageUrl (2000)     │
                    │ Description (4000)  │
                    │ SourceType (20)     │
                    │ IsDeleted           │
                    │ CreatedAt           │
                    │ UpdatedAt           │
                    └───┬──────┬──────────┘
                        │      │
                   1:N  │      │ 1:N
                        ▼      ▼
          ┌──────────────┐  ┌───────────────────┐
          │ RecipeTags   │  │ RecipeIngredients  │
          ├──────────────┤  ├───────────────────┤
          │ Id PK        │  │ Id PK             │
          │ RecipeId FK  │  │ RecipeId FK       │
          │ Name (50)    │  │ Name (100)        │
          └──────────────┘  │ Quantity (50)     │
                            │ Unit (30)         │
                            └───────────────────┘

┌─────────────────────┐     ┌─────────────────────┐
│ MealPlans           │     │ MealPlanRecipes     │
├─────────────────────┤     ├─────────────────────┤
│ Id (int) PK         │────►│ Id (int) PK         │
│ FamilyGroupId FK    │ 1:N │ MealPlanId FK       │
│ Date (DateOnly)     │     │ RecipeId FK         │
│ MealType (string)   │     └─────────────────────┘
│ CreatedAt           │     UQ: (MealPlanId, RecipeId)
│ UpdatedAt           │
└─────────────────────┘
UQ: (FamilyGroupId, Date, MealType)

┌─────────────────────────┐
│ ShoppingListChecks      │
├─────────────────────────┤
│ Id (int) PK             │
│ FamilyGroupId FK        │
│ WeekStartDate (DateOnly)│
│ IngredientName (100)    │
│ IsChecked               │
└─────────────────────────┘
UQ: (FamilyGroupId, WeekStartDate, IngredientName)
```

### インデックス戦略

| テーブル | インデックス | 種別 | 目的 |
|----------|-------------|------|------|
| FamilyGroups | InviteCode | UNIQUE | 招待コード検索 |
| Recipes | FamilyGroupId | INDEX | グループ別レシピ取得 |
| MealPlans | (FamilyGroupId, Date, MealType) | UNIQUE | 献立一意制約 |
| MealPlanRecipes | (MealPlanId, RecipeId) | UNIQUE | 重複防止 |
| ShoppingListChecks | (FamilyGroupId, WeekStartDate, IngredientName) | UNIQUE | チェック状態一意制約 |

### カスケード削除ルール

| 親テーブル | 子テーブル | 削除動作 |
|-----------|-----------|----------|
| FamilyGroup | Recipe | CASCADE |
| FamilyGroup | MealPlan | CASCADE |
| FamilyGroup | ShoppingListCheck | CASCADE |
| FamilyGroup | User.FamilyGroupId | SET NULL |
| Recipe | RecipeTag | CASCADE |
| Recipe | RecipeIngredient | CASCADE |
| MealPlan | MealPlanRecipe | CASCADE |
| Recipe | MealPlanRecipe | NO ACTION |

---

## 2. API 設計詳細

### 共通レスポンスエンベロープ

```json
// 成功
{
  "success": true,
  "data": { ... },
  "error": null
}

// エラー
{
  "success": false,
  "data": null,
  "error": "エラーメッセージ"
}
```

### エンドポイント詳細

#### Auth API

**POST /api/auth/register**
```
Request:  { "email": "string", "password": "string" }
Response: { "success": true, "data": { "accessToken": "string", "refreshToken": "string", "expiresAt": "datetime" } }
Errors:   400 (バリデーション), 409 (メール重複)
```

**POST /api/auth/login**
```
Request:  { "email": "string", "password": "string" }
Response: { "success": true, "data": { "accessToken": "string", "refreshToken": "string", "expiresAt": "datetime" } }
Errors:   400 (バリデーション), 401 (認証失敗)
```

**POST /api/auth/refresh**
```
Request:  { "refreshToken": "string" }
Response: { "success": true, "data": { "accessToken": "string", "refreshToken": "string", "expiresAt": "datetime" } }
Errors:   400 (バリデーション), 401 (トークン無効)
```

#### Groups API

**POST /api/groups**
```
Request:  { "name": "string" }
Response: 201 { "success": true, "data": { "id": 1, "name": "string", "inviteCode": "string" } }
Errors:   400 (バリデーション), 409 (既にグループ所属)
Auth:     Required
```

**POST /api/groups/join**
```
Request:  { "inviteCode": "string" }
Response: { "success": true, "data": { "id": 1, "name": "string", "inviteCode": "string" } }
Errors:   404 (コード無効), 409 (既にグループ所属)
Auth:     Required
```

**GET /api/groups/members**
```
Response: { "success": true, "data": [{ "id": "guid", "email": "string" }] }
Auth:     Required (familyGroupId from JWT)
```

#### Recipes API

**GET /api/recipes**
```
Response: { "success": true, "data": [RecipeResponse] }
Auth:     Required
```

**GET /api/recipes/{id}**
```
Response: { "success": true, "data": RecipeResponse }
Errors:   404 (レシピ未発見)
Auth:     Required
```

**POST /api/recipes**
```
Request:  CreateRecipeRequest
Response: 201 { "success": true, "data": RecipeResponse }
Errors:   400 (バリデーション)
Auth:     Required
```

**PUT /api/recipes/{id}**
```
Request:  UpdateRecipeRequest
Response: { "success": true, "data": RecipeResponse }
Errors:   400 (バリデーション), 404 (レシピ未発見)
Auth:     Required
```

**DELETE /api/recipes/{id}**
```
Response: 204 No Content
Errors:   404 (レシピ未発見)
Auth:     Required
```

**POST /api/recipes/fetch-ogp**
```
Request:  { "url": "string" }
Response: { "success": true, "data": { "title": "string", "imageUrl": "string", "description": "string" } }
Errors:   400 (URL 無効)
Auth:     Required
```

**POST /api/recipes/parse-ingredients**
```
Request:  { "text": "string" }
Response: { "success": true, "data": [{ "name": "string", "quantity": "string", "unit": "string" }] }
Auth:     Required
```

#### MealPlans API

**GET /api/mealplans?weekStart={date}**
```
Response: { "success": true, "data": { "weekStart": "date", "plans": [MealPlanResponse] } }
Auth:     Required
```

**PUT /api/mealplans/{date}/{mealType}**
```
Request:  { "recipeIds": [int] }
Response: { "success": true, "data": MealPlanResponse }
Auth:     Required
```

**POST /api/mealplans/auto-generate**
```
Request:  { "weekStart": "date" }
Response: { "success": true, "data": { "weekStart": "date", "plans": [MealPlanResponse] } }
Auth:     Required
```

**POST /api/mealplans/auto-fill**
```
Request:  { "weekStart": "date" }
Response: { "success": true, "data": { "weekStart": "date", "plans": [MealPlanResponse] } }
Auth:     Required
```

#### Shopping API

**GET /api/shopping-list?weekStart={date}**
```
Response: { "success": true, "data": { "weekStart": "date", "items": [ShoppingItemResponse] } }
Auth:     Required
```

**PUT /api/shopping-list/check**
```
Request:  { "weekStart": "date", "ingredientName": "string", "isChecked": true }
Response: { "success": true }
Auth:     Required
```

---

## 3. セキュリティ設計

### 認証フロー
1. クライアント → `POST /api/auth/login` → JWT 取得
2. JWT を `localStorage` に保存
3. 以降のリクエストに `Authorization: Bearer <token>` 付与
4. Middleware が JWT を検証 → ClaimsPrincipal を FunctionContext に設定
5. Function が ClaimsPrincipal から userId / familyGroupId を取得

### データ分離
- すべてのビジネスクエリに `WHERE FamilyGroupId = @groupId` を付与
- Repository レイヤーで groupId フィルタを強制
- 他グループのデータには物理的にアクセス不可能

### セキュリティヘッダー
```json
{
  "X-Robots-Tag": "noindex, nofollow",
  "X-Content-Type-Options": "nosniff",
  "X-Frame-Options": "DENY",
  "Referrer-Policy": "strict-origin-when-cross-origin"
}
```

---

## 4. エラーハンドリング設計

### HTTP ステータスコード

| コード | 用途 |
|--------|------|
| 200 | 正常完了 |
| 201 | リソース作成成功 |
| 204 | 削除成功（本文なし）|
| 400 | バリデーションエラー |
| 401 | 認証エラー（未認証 / トークン無効）|
| 403 | 認可エラー（権限なし）|
| 404 | リソース未発見 |
| 409 | 競合（重複登録等）|
| 500 | サーバーエラー |

### エラーメッセージ方針
- ユーザー向けメッセージは日本語
- 内部エラーの詳細はクライアントに漏洩させない
- サーバーログに詳細を記録（ILogger）

---

## 5. OGP メタデータ取得設計

```
クライアント                         API
    │                               │
    │  POST /api/recipes/fetch-ogp   │
    │  { "url": "https://..." }      │
    │ ─────────────────────────────► │
    │                               │ URL バリデーション
    │                               │ HttpClient で HTML 取得
    │                               │ og:title, og:image, og:description 抽出
    │  { title, imageUrl, desc }    │
    │ ◄───────────────────────────── │
```

- URL フェッチはサーバーサイドのみ（SSRF 対策）
- プライベート IP / ローカルホストへのリクエストを禁止
- レスポンスサイズ上限: 1MB
- タイムアウト: 10 秒

---

## 6. 自動献立生成アルゴリズム

```
Input:  weekStart (DateOnly), familyGroupId (int)
Output: 14 セル分の献立 (7日 × 昼食/夕食)

Algorithm:
1. グループの全レシピを取得（IsDeleted = false）
2. レシピが 0 件の場合、エラーを返す
3. レシピリストをシャッフル
4. 各セル (日×食事タイプ) にラウンドロビンでレシピを割り当て
   - 連続する日に同じレシピが来ないよう調整
5. MealPlan + MealPlanRecipe を作成して保存

auto-fill の場合:
1. 既存の MealPlan を取得
2. 空きセルのみを特定
3. 空きセルに対してのみ上記アルゴリズムを適用
```

---

## 7. 買い物リスト集約アルゴリズム

```
Input:  weekStart (DateOnly), familyGroupId (int)
Output: 材料リスト (材料名 → 合計数量)

Algorithm:
1. 指定週の全 MealPlan を取得
2. 各 MealPlanRecipe → Recipe → RecipeIngredients を展開
3. 材料名でグルーピング
4. 同一材料・同一単位の数量を合算（数値の場合）
5. 数値でない数量はそのまま列挙
6. ShoppingListCheck テーブルからチェック状態を取得
7. 材料リストとチェック状態をマージして返却
```
