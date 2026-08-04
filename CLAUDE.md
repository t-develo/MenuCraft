# MenuCraft — Claude Code Instructions

<!-- Rules -->
@.claude/rules/common/agents.md
@.claude/rules/common/coding-style.md
@.claude/rules/common/development-workflow.md
@.claude/rules/common/git-workflow.md
@.claude/rules/common/hooks.md
@.claude/rules/common/patterns.md
@.claude/rules/common/performance.md
@.claude/rules/common/security.md
@.claude/rules/common/testing.md
@.claude/rules/dotnet/coding-style.md
@.claude/rules/dotnet/hooks.md
@.claude/rules/dotnet/patterns.md
@.claude/rules/dotnet/security.md
@.claude/rules/dotnet/testing.md
@.claude/rules/javascript/coding-style.md
@.claude/rules/javascript/hooks.md
@.claude/rules/javascript/patterns.md
@.claude/rules/javascript/security.md
@.claude/rules/javascript/testing.md

## Project Overview

**MenuCraft** は、Web / YouTube / Instagram で見つけたレシピを一箇所に集約し、1週間の昼食・夕食の献立を自動＋手動で組み立てられる個人向け献立管理アプリ。買い物リストの自動生成にも対応する。

- 想定ユーザー: 個人 + 家族（2〜4人）
- 検索エンジンには露出しない（`noindex`、認証必須）
- 同時接続数: 最大5程度

---

## Development Policy

### Accuracy Over Speed（速さより正確性を重視）

**速さより正確性を重視すること。**

- コードを書く前に、既存のコードをよく読んで理解してから変更を加える
- 不確かな場合は、推測で進めず、ユーザーに確認する
- 変更を加える前に影響範囲を十分に把握する
- テストが存在する場合は必ず実行し、すべてパスすることを確認する
- 小さくても正確な実装を、速くても不正確な実装より優先する

Prioritize correctness over speed at all times:
- Read and understand existing code thoroughly before making changes
- When uncertain, ask the user rather than guessing
- Understand the full scope of impact before making changes
- Always run tests when they exist and ensure all pass
- Prefer a small but correct implementation over a fast but incorrect one

---

## Tech Stack

| Layer | Technology | Notes |
|---|---|---|
| Frontend | Vanilla JS SPA | fetch API, DOM操作中心。ライブラリ最小限 |
| Hosting (static) | nginx (ラズパイ) / Azure Storage Account | ラズパイ主体。Azure 資産も維持 |
| Backend (API) | Azure Functions (.NET 8 Isolated Worker) | HTTP Trigger。ラズパイでは Core Tools + systemd で常駐 |
| Database | **SQLite** (ラズパイ) / Azure SQL Database | `Database:Provider` 設定で切替（既定 `SqlServer`） |
| Auth | ASP.NET Identity | JWT Bearer Token。メール + パスワード (MVP) |
| CI/CD | GitHub Actions | `ci.yml` のみ自動。デプロイは手動実行 (`workflow_dispatch`) |

### ローカル実行（Raspberry Pi）

主たる運用環境。`deploy/raspi/setup.sh` で nginx + SQLite + systemd を一括構成する。
詳細は `docs/RASPBERRY_PI.md` を参照。

- **64bit OS 必須**（.NET 8 は 32bit ARM 非対応）
- SQLite のスキーマは起動時に `EnsureCreated()` で生成される。**モデル変更には追従しない**
- nginx が `/api/` を `127.0.0.1:7071` へ中継するため同一オリジン。`AllowedOrigins` は不要

---

## Repository Structure

```
menucraft/
├── deploy/raspi/        # Raspberry Pi 用 (setup.sh, deploy.sh, backup.sh, systemd, nginx)
├── src/
│   ├── client/          # Vanilla JS SPA → Azure Storage Account 静的 Web サイト
│   │   ├── index.html
│   │   ├── 404.html     # SPA フォールバック用 (index.html のコピー)
│   │   ├── css/
│   │   └── js/
│   │       ├── config.js  # API_BASE_URL 等の環境設定
│   │       └── api/
│   └── api/             # Azure Functions (.NET 8 Isolated Worker)
│       ├── Functions/
│       ├── Services/
│       ├── Middleware/   # JWT認証 + セキュリティヘッダー
│       ├── Models/
│       └── host.json
├── infra/               # Bicep テンプレート (Storage Account, Functions App, SQL)
└── .github/workflows/   # CI/CD (deploy-frontend.yml, deploy-api.yml)
```

---

## Commands

### Backend (Azure Functions / .NET 10)

```bash
# 依存関係の復元
dotnet restore src/api/

# ビルド
dotnet build src/api/

# ローカル実行 (Azure Functions Core Tools が必要)
cd src/api && func start

# テスト実行
dotnet test
```

### Frontend

```bash
# ビルドステップなし（Vanilla JS）
# ローカル開発は Live Server 等でファイル配信
```

### Infrastructure (Bicep)

```bash
# Azure へのデプロイ (CI/CD が自動実行)
az deployment group create --resource-group <rg> --template-file infra/main.bicep
```

---

## API Endpoints Overview

| Category | Method | Path | Description |
|---|---|---|---|
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
| Management | GET/PUT/DELETE | `/api/management/**` | 管理画面 API（Admin のみ） |

> **ルート命名の制約**: `admin/` で始まる HTTP Trigger ルートは使用できない。
> Azure Functions ホストが `/admin/*` を組み込みの管理エンドポイントとして予約しており、
> 「The specified route conflicts with one or more built in routes」で関数の登録に失敗する。
> 管理系は `management/` を使うこと。

---

## Data Model (Key Entities)

```
User            → FamilyGroup (N:1)
Recipe          → FamilyGroup (N:1), RecipeTag (1:N), RecipeIngredient (1:N)
MealPlan        → FamilyGroup (N:1), MealPlanRecipe (1:N) → Recipe (N:1)
ShoppingListCheck → FamilyGroup (N:1)
```

Key constraints:
- `MealPlan`: `(FamilyGroupId, Date, MealType)` にユニーク制約
- `MealPlanRecipe`: `(MealPlanId, RecipeId)` にユニーク制約
- `ShoppingListCheck`: `(FamilyGroupId, WeekStartDate, IngredientName)` にユニーク制約

---

## Architecture Notes

- フロントエンドは Azure Storage Account 静的 Web サイトでホスティング
- API は独立した Azure Functions (Consumption Plan) にデプロイ。CORS 設定でフロントエンドのオリジンを許可
- セキュリティヘッダー（X-Robots-Tag, X-Content-Type-Options, X-Frame-Options, Referrer-Policy）は API の `SecurityHeadersMiddleware` で付与
- フロントエンドの `config.js` で `API_BASE_URL` を設定し、クロスオリジン API 呼び出しに対応
- SPA ルーティングは Storage Account の 404 ドキュメントを `404.html`（= `index.html` のコピー）に設定して対応
- コールドスタートあり（個人利用のため許容）
- Last Write Wins で競合解決（同時編集は少人数のため頻度低）
- 献立履歴は過去1ヶ月分保持、それ以降は論理削除 + 定期クリーンアップ

---

## Development Phases

- **Phase 1 (MVP)**: 認証、家族グループ、レシピ登録(OGP)、献立ボード、自動生成、買い物リスト
- **Phase 2**: レシピ検索・フィルタ、埋め込みプレビュー、献立履歴閲覧
- **Phase 3**: Google ログイン、PWA 化、外部連携、単位正規化

---

## Git Workflow

- Default branch: `main`
- Feature branches: `feature/<description>`
- フロントエンドと API は独立してデプロイ
- 常に明確なコミットメッセージを書く

---

## Security

- 全ページ認証必須（ログイン画面以外）
- `noindex` + `robots.txt` で検索エンジンに露出しない
- HTTPS 必須（Azure Functions は既定で HTTPS、Storage Account はカスタムドメイン利用時に別途対応）
- パスワード: PBKDF2 (ASP.NET Identity デフォルト)
- API: JWT Bearer Token 認証
