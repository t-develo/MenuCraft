# Cycle 1: プロジェクトスキャフォールディング & CI/CD

## 目的
プロジェクトの基盤を構築し、ビルド・テスト・デプロイのパイプラインを確立する。

## 成果物
- [x] `.gitignore`（.NET, Node, IDE, Azure Functions 用）
- [x] Azure Functions .NET 10 Isolated Worker プロジェクト (`src/api/MenuCraft.Api.csproj`)
- [x] `Program.cs`（ホスト設定）
- [x] `host.json`（Azure Functions 設定）
- [x] `local.settings.json`（ローカル開発設定、Git 管理外）
- [x] `AppDbContext` プレースホルダー
- [x] `HealthFunction`（`GET /api/health`）
- [x] xUnit テストプロジェクト (`tests/api.Tests/`)
- [x] `HealthFunctionTests`
- [x] フロントエンドスケルトン (`src/client/`)
  - `index.html`, `login.html`, `register.html`
  - `css/style.css`
  - `js/api/apiFetch.js`, `js/app.js`, `js/api/auth.js`
  - `robots.txt`
- [x] `staticwebapp.config.json`（SWA ルーティング、セキュリティヘッダー）
- [x] GitHub Actions ワークフロー (`.github/workflows/azure-swa.yml`)
- [x] Bicep テンプレート (`infra/main.bicep`)

## 依存関係
- なし（最初のサイクル）

## リスク
- .NET 10 プレビュー SDK がCI環境で利用可能か
- Azure SQL Free Tier の制限
