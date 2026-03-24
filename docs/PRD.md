# MenuCraft — PRD（プロダクト要件定義書）

## 1. プロダクト概要

**MenuCraft** は、Web・YouTube・Instagram で見つけたレシピを一箇所に集約し、1週間の昼食・夕食の献立を自動＋手動で組み立てられる個人向け献立管理アプリ。買い物リストの自動生成にも対応する。

### ビジョン

「レシピを見つける → 献立を組む → 買い物リストを持って出かける」を1つのアプリでシームレスに完結させる。

---

## 2. ターゲットユーザー

| 属性 | 詳細 |
|------|------|
| ユーザー層 | 個人 + 家族（2〜4人） |
| 想定人数 | 1〜5人の同時接続 |
| 利用シーン | 週末に1週間分の献立を計画、平日に買い物リストを確認 |
| 技術レベル | 一般的なスマートフォン/PC利用者 |

---

## 3. 解決する課題

1. **レシピの散在**: Web、YouTube、Instagramに保存したレシピがバラバラで見つけにくい
2. **献立の決定疲れ**: 毎日「今日何作ろう？」と悩む時間が長い
3. **買い物の非効率**: 何を買えばいいか分からず、重複購入や買い忘れが発生
4. **家族間の共有**: 家族でレシピや献立を共有する手段がない

---

## 4. 機能要件

### Phase 1 (MVP)

#### F-001: ユーザー認証
- メール + パスワードによるユーザー登録
- JWT Bearer Token によるログイン/ログアウト
- トークンリフレッシュ機能
- 全ページ認証必須（ログイン/登録画面を除く）

#### F-002: 家族グループ管理
- グループ作成（グループ名を入力、招待コード自動生成）
- 招待コードによるグループ参加
- メンバー一覧表示
- 1ユーザー = 1グループ制約

#### F-003: レシピ登録・管理
- 手動でのレシピ登録（タイトル、URL、画像、説明、タグ、材料）
- URL入力時のOGPメタデータ自動取得（タイトル、画像）
- レシピの編集・削除
- レシピ一覧表示（グループ単位）
- ソースタイプ自動判定（Manual / Web / YouTube / Instagram）

#### F-004: 材料テキスト解析
- テキスト入力から材料名・数量・単位を自動パース
- 「鶏もも肉 300g」→ { name: "鶏もも肉", quantity: "300", unit: "g" }

#### F-005: 献立ボード
- 1週間（月〜日）× 2食（昼食・夕食）のグリッド表示
- 週の開始日を指定して表示切替
- 各セルにレシピをドラッグ＆ドロップまたは選択で割り当て
- 各セルのレシピ解除

#### F-006: 自動献立生成
- ボタン1つで1週間分の献立を自動生成
- 登録済みレシピからランダムに選択
- 「空きセルのみ自動埋め」機能（既に入っているセルは変更しない）

#### F-007: 買い物リスト
- 指定した週の献立から材料を自動集約
- 同一材料の数量合算
- チェックボックスで購入済みマーク
- チェック状態の永続化

### Phase 2（予定）

- レシピ検索・フィルタリング（タグ、タイトル、材料）
- 埋め込みプレビュー（YouTube、Instagram の埋め込み表示）
- 献立履歴閲覧（過去の週の献立を閲覧）

### Phase 3（予定）

- Google ログイン対応
- PWA 化（オフライン対応、ホーム画面追加）
- 外部サービス連携
- 材料単位の正規化（"g" と "グラム" を統一）

---

## 5. 非機能要件

### セキュリティ
- 認証必須（未認証ユーザーはログイン画面にリダイレクト）
- `noindex` + `robots.txt` で検索エンジンに露出しない
- HTTPS 必須（SWA が無料 TLS 証明書を自動提供）
- パスワード: PBKDF2（ASP.NET Identity デフォルト）
- JWT Bearer Token 認証（API）
- 全 API エンドポイントで familyGroupId によるデータ分離

### パフォーマンス
- コールドスタートあり（個人利用のため許容）
- Last Write Wins による競合解決（同時編集は頻度低）
- 献立履歴は過去1ヶ月分保持、以降は論理削除 + 定期クリーンアップ

### 可用性
- Azure SWA Free Tier（SLA なし、個人利用で許容）
- Azure SQL Free Tier（月間利用制限あり）

### アクセシビリティ
- レスポンシブデザイン（モバイル・デスクトップ対応）
- 日本語 UI

---

## 6. データモデル概要

```
User            → FamilyGroup (N:1)
Recipe          → FamilyGroup (N:1), RecipeTag (1:N), RecipeIngredient (1:N)
MealPlan        → FamilyGroup (N:1), MealPlanRecipe (1:N) → Recipe (N:1)
ShoppingListCheck → FamilyGroup (N:1)
```

### 主要制約
- `MealPlan`: `(FamilyGroupId, Date, MealType)` ユニーク制約
- `MealPlanRecipe`: `(MealPlanId, RecipeId)` ユニーク制約
- `ShoppingListCheck`: `(FamilyGroupId, WeekStartDate, IngredientName)` ユニーク制約

---

## 7. API エンドポイント一覧

| カテゴリ | Method | Path | 説明 |
|----------|--------|------|------|
| Auth | POST | `/api/auth/register` | ユーザー登録 |
| Auth | POST | `/api/auth/login` | ログイン（JWT発行）|
| Auth | POST | `/api/auth/refresh` | トークンリフレッシュ |
| Groups | POST | `/api/groups` | 家族グループ作成 |
| Groups | POST | `/api/groups/join` | 招待コードで参加 |
| Groups | GET | `/api/groups/members` | メンバー一覧 |
| Recipes | GET | `/api/recipes` | レシピ一覧 |
| Recipes | POST | `/api/recipes` | レシピ登録 |
| Recipes | PUT | `/api/recipes/{id}` | レシピ更新 |
| Recipes | DELETE | `/api/recipes/{id}` | レシピ削除 |
| Recipes | POST | `/api/recipes/fetch-ogp` | URL → OGP メタデータ取得 |
| Recipes | POST | `/api/recipes/parse-ingredients` | テキスト → 材料パース |
| MealPlans | GET | `/api/mealplans?weekStart={date}` | 週間献立取得 |
| MealPlans | PUT | `/api/mealplans/{date}/{mealType}` | 1食分の献立更新 |
| MealPlans | POST | `/api/mealplans/auto-generate` | 自動献立生成 |
| MealPlans | POST | `/api/mealplans/auto-fill` | 空きセルのみ自動埋め |
| Shopping | GET | `/api/shopping-list?weekStart={date}` | 買い物リスト生成・取得 |
| Shopping | PUT | `/api/shopping-list/check` | チェック状態更新 |

---

## 8. 成功指標

MVP の成功基準:
1. ユーザーが Web/YouTube/Instagram のレシピ URL を保存できる
2. 1週間分の献立を手動・自動で作成できる
3. 献立に基づいた買い物リストが生成される
4. 家族間でレシピ・献立を共有できる
