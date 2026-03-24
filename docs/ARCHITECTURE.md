# MenuCraft — アーキテクチャ設計書

## 1. システム全体構成

```
┌──────────────────────────────────────────────────┐
│                    Client                        │
│  ┌────────────────────────────────────────────┐  │
│  │     Azure Static Web Apps (Free Tier)      │  │
│  │  ┌──────────────────────────────────────┐  │  │
│  │  │   Vanilla JS SPA                     │  │  │
│  │  │   - index.html (メインページ)          │  │  │
│  │  │   - login.html / register.html       │  │  │
│  │  │   - css/style.css                    │  │  │
│  │  │   - js/ (API client, pages, components)│ │  │
│  │  └──────────────────────────────────────┘  │  │
│  │                    │                       │  │
│  │              /api/* ルーティング             │  │
│  │                    ▼                       │  │
│  │  ┌──────────────────────────────────────┐  │  │
│  │  │   Azure Functions (.NET 10)          │  │  │
│  │  │   Isolated Worker (Managed)          │  │  │
│  │  │   - AuthFunction                     │  │  │
│  │  │   - GroupFunction                    │  │  │
│  │  │   - RecipeFunction                   │  │  │
│  │  │   - MealPlanFunction                 │  │  │
│  │  │   - ShoppingFunction                 │  │  │
│  │  └──────────────┬───────────────────────┘  │  │
│  └─────────────────┼─────────────────────────┘  │
│                    │                             │
│                    ▼                             │
│  ┌──────────────────────────────────────────┐   │
│  │   Azure SQL Database (Free Tier)         │   │
│  │   - ASP.NET Identity テーブル             │   │
│  │   - アプリケーション テーブル               │   │
│  └──────────────────────────────────────────┘   │
└──────────────────────────────────────────────────┘
```

---

## 2. テクノロジースタック

| レイヤー | 技術 | 選定理由 |
|----------|------|----------|
| Frontend | Vanilla JS SPA | ビルドステップ不要、シンプル、学習コスト低 |
| Hosting | Azure Static Web Apps (Free) | CDN、無料TLS、PR プレビュー環境 |
| API | Azure Functions (.NET 10 Isolated) | SWA マネージド統合、コールドスタート許容 |
| Database | Azure SQL Database (Free) | RDB、Identity 統合、Free Tier |
| Auth | ASP.NET Identity + JWT | 標準的な認証基盤、PBKDF2 ハッシュ |
| ORM | Entity Framework Core 10 | .NET 標準 ORM、Code First |
| CI/CD | GitHub Actions | SWA ネイティブ統合、PR 自動デプロイ |
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
├── login.html          # ログインページ
├── register.html       # 登録ページ
├── robots.txt          # 検索エンジン拒否
├── css/
│   └── style.css       # グローバルスタイル
└── js/
    ├── app.js           # エントリーポイント、ルーティング
    ├── api/
    │   ├── apiFetch.js  # HTTP クライアント（JWT 付与、401 処理）
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
- `staticwebapp.config.json` で `/api/*` 以外は `index.html` にフォールバック

---

## 7. デプロイアーキテクチャ

```
GitHub (main branch)
    │
    │  push / PR merge
    ▼
GitHub Actions
    │
    ├── dotnet restore / build / test
    │
    └── Azure/static-web-apps-deploy@v1
        ├── app_location: src/client    → CDN にデプロイ
        └── api_location: src/api       → Managed Functions にデプロイ
```

### PR プレビュー環境
- PR ごとにステージング環境が自動生成
- PR クローズ時に自動削除

---

## 8. アーキテクチャ上の判断と根拠

| 判断 | 根拠 |
|------|------|
| Vanilla JS（フレームワークなし） | ビルドステップ不要、SWA Free Tier と最適な相性 |
| Azure Functions Isolated Worker | SWA マネージド統合、HTTP トリガーのみで十分 |
| JWT in localStorage | Cookie 不要（same-origin API）、SPA に適合 |
| Last Write Wins | 同時編集が極めて少ない（最大5人、家族利用） |
| 論理削除 + 定期クリーンアップ | Free Tier のストレージ制限対応 |
| EF Core Code First | スキーマ管理の一元化、マイグレーション自動生成 |
| Repository Pattern | テスト容易性、データアクセスの抽象化 |

---

## 9. 制約と制限

| 制約 | 影響 | 対策 |
|------|------|------|
| SWA Free Tier | カスタムドメイン制限あり | Free Tier の範囲で運用 |
| Azure SQL Free Tier | 月間 100,000 vCore 秒 | オートポーズ 60 秒、読み取り最適化 |
| Managed Functions | HTTP トリガーのみ | タイマー必要時は別途 Consumption Plan |
| コールドスタート | 初回リクエストが遅い | 個人利用のため許容 |
| Vanilla JS | 大規模化に限界あり | 機能範囲を MVP に絞る |
