# MenuCraft — アーキテクチャ設計書

## 1. システム全体構成

```
┌─────────────────────────────────┐    ┌─────────────────────────────────┐
│  Azure Storage Account          │    │  Azure Functions                │
│  静的 Web サイト                  │    │  Consumption Plan (.NET 10)      │
│  ┌───────────────────────────┐  │    │  ┌───────────────────────────┐  │
│  │  Vanilla JS SPA           │  │    │  │  Isolated Worker          │  │
│  │  - index.html             │  │    │  │  - AuthFunction           │  │
│  │  - 404.html (SPA fallback)│  │    │  │  - GroupFunction          │  │
│  │  - login.html / register  │  │    │  │  - RecipeFunction         │  │
│  │  - css/style.css          │──┼───►│  │  - MealPlanFunction       │  │
│  │  - js/config.js           │  │    │  │  - ShoppingFunction       │  │
│  │  - js/api/, pages/, ...   │  │CORS│  │  - SecurityHeaders MW     │  │
│  └───────────────────────────┘  │    │  │  - JwtAuth MW             │  │
└─────────────────────────────────┘    │  └─────────────┬─────────────┘  │
                                       └────────────────┼────────────────┘
                                                        │
                                                        ▼
                                       ┌─────────────────────────────────┐
                                       │  Azure SQL Database (Free Tier) │
                                       │  - ASP.NET Identity テーブル     │
                                       │  - アプリケーション テーブル       │
                                       └─────────────────────────────────┘
```

---

## 2. テクノロジースタック

| レイヤー | 技術 | 選定理由 |
|----------|------|----------|
| Frontend | Vanilla JS SPA | ビルドステップ不要、シンプル、学習コスト低 |
| Hosting (Frontend) | Azure Storage Account 静的 Web サイト | 低コスト、独立デプロイ |
| API | Azure Functions (.NET 10 Isolated) | Consumption Plan、独立デプロイ、CORS 対応 |
| Database | Azure SQL Database (Free) | RDB、Identity 統合、Free Tier |
| Auth | ASP.NET Identity + JWT | 標準的な認証基盤、PBKDF2 ハッシュ |
| ORM | Entity Framework Core 8 | .NET 標準 ORM、Code First |
| CI/CD | GitHub Actions | フロントエンド・API 独立デプロイ |
| IaC | Bicep | Azure ネイティブ、ARM テンプレートの上位互換 |

---

## 3. レイヤーアーキテクチャ

```
┌─────────────────────────────────┐
│  Functions Layer (HTTP Trigger) │  ← リクエスト受付、レスポンス生成
│  AuthFunction, RecipeFunction...│
├─────────────────────────────────┤
│  Middleware Layer               │  ← JWT 認証、横断的関心事
│  JwtAuthenticationMiddleware    │
├─────────────────────────────────┤
│  Service Layer                  │  ← ビジネスロジック
│  RecipeService, GroupService... │
├─────────────────────────────────┤
│  Repository Layer               │  ← データアクセス抽象化
│  RecipeRepository, GroupRepo... │
├─────────────────────────────────┤
│  Data Layer (EF Core)           │  ← DB スキーマ、マイグレーション
│  AppDbContext                   │
├─────────────────────────────────┤
│  Model Layer                    │  ← エンティティ、DTO、Enum
│  Recipe, MealPlan, User...      │
└─────────────────────────────────┘
```

### 各レイヤーの責務

| レイヤー | 責務 | 依存方向 |
|----------|------|----------|
| Functions | HTTP リクエスト/レスポンス変換、入力バリデーション | → Service |
| Middleware | JWT 検証、ClaimsPrincipal 生成 | → Configuration |
| Service | ビジネスロジック、DTO ↔ Entity マッピング | → Repository |
| Repository | CRUD 操作、クエリ構築 | → DbContext |
| Data | DB スキーマ定義、リレーション設定 | → Models |
| Models | ドメインエンティティ、列挙型 | なし（最下層）|

---

## 4. 認証・認可フロー

```
クライアント                    API (Azure Functions)
    │                               │
    │  POST /api/auth/login          │
    │  { email, password }           │
    │ ─────────────────────────────► │
    │                               │ Identity: パスワード検証
    │                               │ JwtTokenService: JWT 生成
    │  { accessToken, refreshToken } │
    │ ◄───────────────────────────── │
    │                               │
    │  GET /api/recipes              │
    │  Authorization: Bearer <JWT>   │
    │ ─────────────────────────────► │
    │                               │ Middleware: JWT 検証
    │                               │ ClaimsPrincipal → context.Items
    │                               │ Function: familyGroupId 取得
    │  { data: [...recipes] }       │
    │ ◄───────────────────────────── │
```

### 認証不要エンドポイント
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `GET /api/health`

### 認証必要エンドポイント
- 上記以外のすべての `/api/*` エンドポイント

---

## 5. データ分離戦略

すべてのビジネスデータは `FamilyGroupId` で分離される。

```
リクエスト
    │
    ▼
JWT から familyGroupId を取得
    │
    ▼
Repository は familyGroupId で WHERE 句フィルタ
    │
    ▼
他グループのデータにはアクセス不可
```

---

## 6. フロントエンドアーキテクチャ

### ファイル構成
```
src/client/
├── index.html          # メイン SPA シェル
├── 404.html            # SPA フォールバック (index.html のコピー)
├── login.html          # ログインページ
├── register.html       # 登録ページ
├── robots.txt          # 検索エンジン拒否
├── css/
│   └── style.css       # グローバルスタイル
└── js/
    ├── config.js        # 環境設定 (API_BASE_URL)
    ├── app.js           # エントリーポイント、ルーティング
    ├── api/
    │   ├── apiFetch.js  # HTTP クライアント（JWT 付与、401 処理、クロスオリジン対応）
    │   ├── auth.js      # 認証 API
    │   ├── recipes.js   # レシピ API
    │   ├── mealplans.js # 献立 API
    │   └── shopping.js  # 買い物リスト API
    ├── pages/
    │   ├── groupSetup.js    # グループ設定
    │   ├── recipes.js       # レシピ一覧
    │   ├── mealPlanBoard.js # 献立ボード
    │   └── shoppingList.js  # 買い物リスト
    └── components/
        ├── recipeCard.js    # レシピカード
        └── recipeForm.js    # レシピフォーム（モーダル）
```

### ステート管理
- `localStorage`: JWT トークン永続化
- ページ単位の関数スコープ: 各ページ内のローカル状態

### ルーティング
- HTML ファイルベース（SPA ナビゲーションフォールバック設定済み）
- Azure Storage Account の 404 ドキュメントに `404.html`（= `index.html` のコピー）を設定して SPA フォールバックを実現

---

## 7. デプロイアーキテクチャ

```
GitHub (main branch)
    │
    │  push / PR merge
    ▼
GitHub Actions
    │
    ├── deploy-frontend.yml (src/client 変更時)
    │   └── az storage blob upload-batch → Storage Account $web コンテナ
    │
    └── deploy-api.yml (src/api 変更時)
        ├── dotnet restore / build / test
        └── Azure/functions-action@v1 → Azure Functions App
```

---

## 8. アーキテクチャ上の判断と根拠

| 判断 | 根拠 |
|------|------|
| Vanilla JS（フレームワークなし） | ビルドステップ不要、Storage Account 静的 Web サイトと相性良好 |
| Azure Functions Isolated Worker | Consumption Plan で独立デプロイ、HTTP トリガー |
| JWT in localStorage | SPA に適合、API 呼び出し時に Bearer ヘッダーで送信 |
| Last Write Wins | 同時編集が極めて少ない（最大5人、家族利用） |
| 論理削除 + 定期クリーンアップ | Free Tier のストレージ制限対応 |
| EF Core Code First | スキーマ管理の一元化、マイグレーション自動生成 |
| Repository Pattern | テスト容易性、データアクセスの抽象化 |

---

## 9. 制約と制限

| 制約 | 影響 | 対策 |
|------|------|------|
| Storage Account 静的 Web サイト | SPA フォールバックに制限あり | 404.html で対応 |
| Azure SQL Free Tier | 月間 100,000 vCore 秒 | オートポーズ 60 秒、読み取り最適化 |
| Consumption Plan Functions | コールドスタートあり | 個人利用のため許容 |
| クロスオリジン構成 | CORS 設定が必要 | Functions App + SecurityHeadersMiddleware で対応 |
| Vanilla JS | 大規模化に限界あり | 機能範囲を MVP に絞る |
