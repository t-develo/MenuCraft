# MenuCraft

Web / YouTube / Instagram で見つけたレシピを一箇所に集約し、1週間の昼食・夕食の献立を自動＋手動で組み立てられる個人向け献立管理アプリ。買い物リストの自動生成にも対応する。

- **想定ユーザー**: 個人 + 家族（2〜4人）
- **認証必須**: 検索エンジン非露出（`noindex`、JWT認証）
- **同時接続数**: 最大5程度

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Vanilla JS SPA（ビルドステップなし） |
| Hosting (Frontend) | nginx（ラズパイ） / Azure Storage Account 静的 Web サイト |
| Backend (API) | Azure Functions (.NET 10 Isolated Worker) |
| Database | **SQLite**（ラズパイ） / Azure SQL Database (Free tier) |
| Auth | ASP.NET Identity + JWT Bearer Token |
| CI/CD | GitHub Actions（ビルド・テストのみ自動。デプロイは手動実行） |

デプロイ先は `Database:Provider` 設定で切り替えます（`Sqlite` / `SqlServer`、既定は `SqlServer`）。

---

## Raspberry Pi でのローカル実行

家庭内 LAN の Raspberry Pi 上で単体稼働させられます（**64bit OS 必須**）。

```bash
git clone https://github.com/t-develo/MenuCraft.git
cd MenuCraft
sudo ./deploy/raspi/setup.sh
```

nginx（静的配信 + `/api/` のリバースプロキシ）、SQLite、systemd サービス登録、
再起動時の自動起動、日次バックアップまでを一括で設定します。
セットアップ後は `http://menucraft.local/` でアクセスし、新規登録した最初のユーザーが管理者になります。

更新は `sudo ./deploy/raspi/deploy.sh --pull`。

**詳細な手順・運用・トラブルシュートは [docs/RASPBERRY_PI.md](docs/RASPBERRY_PI.md) を参照してください。**

---

## Features

- **認証**: メール＋パスワードで登録・ログイン
- **家族グループ**: グループ作成・招待コードで参加
- **レシピ管理**: URL入力でOGPメタデータ自動取得、材料テキスト自動解析
- **献立ボード**: 週単位の昼食・夕食の献立管理、自動生成・空きセル自動補完
- **買い物リスト**: 献立から材料を自動集計・チェック管理

---

## Getting Started

### 必要なツール

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (デプロイ時)

### ローカル開発

#### 1. バックエンド（Azure Functions）

```bash
# 依存関係の復元
dotnet restore src/api/

# ビルド
dotnet build src/api/

# ローカル設定ファイルを作成
cp src/api/local.settings.json.example src/api/local.settings.json
# local.settings.json を編集して接続文字列・JWTシークレットを設定
#   - SQLite を使う場合:  "Database:Provider": "Sqlite"
#                         "ConnectionStrings:Default": "Data Source=menucraft.db"
#     （スキーマは起動時に自動生成されます）
#   - SQL Server を使う場合: "Database:Provider" を "SqlServer" にして
#     infra/sql/setup.sql を対象DBに適用しておく

# 起動
cd src/api && func start
```

#### 2. フロントエンド（Vanilla JS）

ビルドステップはありません。以下のいずれかで配信できます。

- **VSCode Live Server** 拡張: `src/client/index.html` を右クリック → 「Open with Live Server」
- **Python**: `python -m http.server 5500 --directory src/client`

> **Note**: ローカル開発時は `src/client/js/config.js` の `API_BASE_URL` を空文字（デフォルト）のままにし、Azure Functions Core Tools (`func start`) を別ターミナルで起動してください。本番環境では Azure Functions の URL を設定します。

### テスト実行

#### バックエンド単体テスト

```bash
dotnet test tests/api.Tests/
```

#### E2Eテスト（Playwright）

```bash
# Playwright のインストール（初回のみ）
npm install
npx playwright install chromium

# E2Eテスト実行（要: ローカルサーバー起動済み）
BASE_URL=http://localhost:7071 npm run test:e2e
```

---

## Infrastructure (Bicep)

Azure リソース（Storage Account、Azure Functions、SQL Server、SQL Database）を Bicep テンプレートで管理しています。

### 前提条件

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) がインストール済みであること

### コマンド

```bash
# Azure にログイン
az login

# サブスクリプションを確認・切り替え（複数ある場合）
az account list --output table
az account set --subscription "<サブスクリプションID>"

# リソースグループを作成（初回のみ）
az group create --name menucraft-rg --location japaneast

# テンプレートの検証（デプロイ前の構文チェック）
az deployment group validate \
  --resource-group menucraft-rg \
  --template-file infra/main.bicep \
  --parameters sqlAdminLogin="<SQLユーザー名>" sqlAdminPassword="<SQLパスワード>"

# What-if（変更内容のプレビュー）
az deployment group what-if \
  --resource-group menucraft-rg \
  --template-file infra/main.bicep \
  --parameters sqlAdminLogin="<SQLユーザー名>" sqlAdminPassword="<SQLパスワード>"

# デプロイ実行
az deployment group create \
  --resource-group menucraft-rg \
  --template-file infra/main.bicep \
  --parameters sqlAdminLogin="<SQLユーザー名>" sqlAdminPassword="<SQLパスワード>"
```

#### パラメータのカスタマイズ（任意）

デフォルト値を変更したい場合は `--parameters` に追記します。

```bash
az deployment group create \
  --resource-group menucraft-rg \
  --template-file infra/main.bicep \
  --parameters \
    sqlAdminLogin="<SQLユーザー名>" \
    sqlAdminPassword="<SQLパスワード>" \
    staticWebAppName="my-menucraft-swa" \
    sqlServerName="my-menucraft-sql" \
    sqlDatabaseName="my-menucraft-db" \
    location="japaneast" \
    swaLocation="eastasia"
```

#### デプロイ結果の確認

```bash
# デプロイ済みリソースの出力値（Static Web App URL、SQL Server FQDN）を確認
az deployment group show \
  --resource-group menucraft-rg \
  --name main \
  --query properties.outputs
```

> **Note**: `sqlAdminPassword` は Azure SQL の要件（8文字以上、大文字・小文字・数字・記号を含む）を満たす必要があります。

---

## Repository Structure

```
menucraft/
├── src/
│   ├── client/              # Vanilla JS SPA → Azure Storage Account 静的 Web サイト
│   │   ├── index.html
│   │   ├── 404.html         # SPA フォールバック (index.html のコピー)
│   │   ├── css/style.css
│   │   └── js/
│   │       ├── config.js    # 環境設定 (API_BASE_URL)
│   │       ├── api/         # APIクライアント (apiFetch, auth, groups, recipes, mealplans, shopping)
│   │       ├── components/  # UIコンポーネント (recipeCard, recipeForm)
│   │       └── pages/       # ページ (groupSetup, recipes, mealPlanBoard, shoppingList)
│   └── api/                 # Azure Functions (.NET 10 Isolated Worker, Consumption Plan)
│       ├── Functions/       # HTTPトリガー関数
│       ├── Services/        # ビジネスロジック
│       ├── Repositories/    # データアクセス層
│       ├── Middleware/      # JWT認証 + セキュリティヘッダー
│       ├── Models/          # エンティティモデル
│       └── Dtos/            # リクエスト/レスポンス DTO
├── tests/
│   ├── api.Tests/           # xUnit バックエンドテスト
│   └── e2e/                 # Playwright E2Eテスト
├── deploy/raspi/            # Raspberry Pi 用 (setup.sh, deploy.sh, systemd, nginx)
├── infra/                   # Bicep テンプレート (Storage Account, Functions, SQL)
├── playwright.config.js
└── .github/workflows/       # ci.yml (自動) / deploy-*.yml (手動実行のみ)
```

---

## API Endpoints

| Category | Method | Path | Description |
|---|---|---|---|
| Auth | POST | `/api/auth/register` | ユーザー登録 |
| Auth | POST | `/api/auth/login` | ログイン（JWT発行）|
| Auth | POST | `/api/auth/refresh` | トークンリフレッシュ |
| Groups | POST | `/api/groups` | 家族グループ作成（新JWTを返す） |
| Groups | POST | `/api/groups/join` | 招待コードで参加（新JWTを返す） |
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
| Management | GET | `/api/management/users` | ユーザー一覧（Admin のみ） |
| Management | PUT | `/api/management/users/{userId}/role` | ロール変更（Admin のみ） |
| Management | GET | `/api/management/groups` | グループ一覧（Admin のみ） |
| Management | PUT/DELETE | `/api/management/groups/{groupId}` | グループ更新・削除（Admin のみ） |

> **注意**: 管理系のルートに `admin/` は使えません。Azure Functions ホストが `/admin/*` を
> 組み込みの管理エンドポイントとして予約しており、`admin` で始まるルートを持つ関数は
> 「The specified route conflicts with one or more built in routes」で登録に失敗し 404 になります。
> そのため `management/` を使用しています。

---

## Security

- 全ページ認証必須（ログイン画面以外）
- `noindex` + `robots.txt` で検索エンジン非露出
- HTTPS 必須（Azure Functions は既定で HTTPS）
- パスワード: PBKDF2 (ASP.NET Identity)
- API: JWT Bearer Token 認証（1時間有効）
- XSS対策: `textContent` 使用、`innerHTML` 不使用
- SQLインジェクション対策: EF Core パラメータ化クエリ
- SSRF対策: OGPフェッチはサーバーサイドのみ（クライアントから外部URL直接取得なし）

---

## Development Phases

- ✅ **Phase 1 (MVP)**: 認証、家族グループ、レシピ登録(OGP)、献立ボード、自動生成、買い物リスト、統合・ナビゲーション
- **Phase 2**: レシピ検索・フィルタ、埋め込みプレビュー、献立履歴閲覧
- **Phase 3**: Google ログイン、PWA化、外部連携、単位正規化
