# Azure IaC & DevOps ベストプラクティス

## Infrastructure as Code (IaC) ツール選択

| ツール | 特徴 | 適用場面 |
|---|---|---|
| **Bicep** | Azure ネイティブ、ARM の抽象化、ARM との完全互換 | Azure 専用環境 |
| **Terraform** | マルチクラウド対応、大きなエコシステム | マルチクラウド・既存 Terraform 環境 |
| ARM Templates | 低レベル、JSON、冗長 | レガシーサポート（新規開発は Bicep を推奨） |
| Pulumi | 汎用プログラミング言語で記述 | コード重視のチーム |

**推奨**: Azure 専用なら **Bicep**、マルチクラウドなら **Terraform**。

---

## Bicep ベストプラクティス

### ファイル構成
```
infra/
├── main.bicep                  # エントリーポイント
├── main.bicepparam             # パラメーターファイル (本番用)
├── main.dev.bicepparam         # パラメーターファイル (開発用)
├── modules/
│   ├── networking.bicep        # ネットワークリソース
│   ├── compute.bicep           # コンピュートリソース
│   ├── storage.bicep           # ストレージリソース
│   └── monitoring.bicep        # 監視リソース
└── scripts/
    └── deploy.sh               # デプロイスクリプト
```

### Bicep のベストプラクティス
```bicep
// パラメーターには型制約と説明を付ける
@description('The environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('The Azure region for deployment')
param location string = resourceGroup().location

@minLength(3)
@maxLength(24)
param storageAccountName string

// 機密情報はキーボルトから取得する (@secure() アノテーション)
@secure()
param adminPassword string

// 環境ごとの設定を変数で管理
var config = {
  dev: {
    sku: 'Standard_B2s'
    instances: 1
  }
  prod: {
    sku: 'Standard_D4s_v3'
    instances: 3
  }
}

var currentConfig = config[environment]

// リソース定義でタグを標準化
var commonTags = {
  Environment: environment
  ManagedBy: 'bicep'
  LastUpdated: utcNow('yyyy-MM-dd')
}
```

### モジュール化
```bicep
// main.bicep からモジュールを呼び出す
module networking 'modules/networking.bicep' = {
  name: 'networkingDeployment'
  params: {
    location: location
    environment: environment
    vnetAddressPrefix: '10.0.0.0/16'
  }
}

module compute 'modules/compute.bicep' = {
  name: 'computeDeployment'
  dependsOn: [networking]  // ネットワーク構築後に実行
  params: {
    location: location
    subnetId: networking.outputs.appSubnetId  // モジュール出力を参照
  }
}
```

### What-if デプロイ（変更プレビュー）
```bash
# 実際に変更する前に変更内容を確認する
az deployment group what-if \
  --resource-group rg-myapp-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam
```

---

## Terraform ベストプラクティス

### ディレクトリ構成
```
terraform/
├── environments/
│   ├── dev/
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── terraform.tfvars
│   └── prod/
│       ├── main.tf
│       ├── variables.tf
│       └── terraform.tfvars
└── modules/
    ├── networking/
    ├── aks/
    └── sql/
```

### State 管理
```hcl
# backend.tf - Azure Blob Storage に State を保存する
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-tfstate"
    storage_account_name = "sttfstatemyapp"
    container_name       = "tfstate"
    key                  = "prod/terraform.tfstate"
  }
}
```

### セキュリティ
```hcl
# 機密情報を tfvars に直書きしない → Key Vault / 環境変数から取得
data "azurerm_key_vault_secret" "db_password" {
  name         = "db-admin-password"
  key_vault_id = azurerm_key_vault.main.id
}

# Terraform Cloud / GitHub Actions でシークレットを環境変数として渡す
# export TF_VAR_db_password=$(az keyvault secret show --name ...)
```

---

## Azure DevOps / GitHub Actions

### CI/CD パイプライン設計原則
1. **コードと同じリポジトリ**でパイプラインを管理する（パイプライン as Code）
2. **環境ごとの承認ゲート**を設定する（本番デプロイは承認必須）
3. **シークレットは変数グループ / GitHub Secrets** で管理する（ハードコードしない）
4. **ブランチ保護**を設定し、PR なしで main にマージできないようにする

### GitHub Actions ワークフロー例（Azure Web App デプロイ）
```yaml
# .github/workflows/deploy.yml
name: Deploy to Azure

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  AZURE_WEBAPP_NAME: app-myapp-prod
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore & Build
        run: |
          dotnet restore
          dotnet build --no-restore --configuration Release

      - name: Run Tests
        run: dotnet test --no-build --configuration Release --logger trx

      - name: Publish
        run: dotnet publish --no-build --configuration Release --output ./publish

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: app-package
          path: ./publish

  deploy-staging:
    needs: build-and-test
    runs-on: ubuntu-latest
    environment: staging
    if: github.ref == 'refs/heads/main'
    steps:
      - uses: actions/download-artifact@v4
        with:
          name: app-package

      - name: Azure Login (OIDC - パスワード不要)
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy to Staging Slot
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          slot-name: staging

  deploy-production:
    needs: deploy-staging
    runs-on: ubuntu-latest
    environment: production  # GitHub Environments で手動承認を設定
    steps:
      - name: Azure Login
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Swap Staging → Production
        run: |
          az webapp deployment slot swap \
            --name ${{ env.AZURE_WEBAPP_NAME }} \
            --resource-group rg-myapp-prod \
            --slot staging \
            --target-slot production
```

### OIDC 認証（サービスプリンシパルのシークレット不要）
GitHub Actions から Azure へは OIDC (OpenID Connect) で認証し、長期シークレットを使わない。

```bash
# GitHub リポジトリに Federated Identity を設定
az ad app create --display-name "github-actions-myapp"
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:myorg/myrepo:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### IaC のデプロイパイプライン
```yaml
# terraform-deploy.yml
jobs:
  terraform-plan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: hashicorp/setup-terraform@v3

      - name: Terraform Init
        run: terraform init
        working-directory: terraform/environments/prod

      - name: Terraform Plan
        run: terraform plan -out=tfplan
        working-directory: terraform/environments/prod
        env:
          ARM_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          ARM_USE_OIDC: true

      - name: Upload Plan
        uses: actions/upload-artifact@v4
        with:
          name: tfplan
          path: terraform/environments/prod/tfplan

  terraform-apply:
    needs: terraform-plan
    environment: production  # 手動承認
    steps:
      - name: Terraform Apply
        run: terraform apply tfplan
```

---

## Azure Container Registry (ACR) ベストプラクティス

### セキュリティ
- **管理者アカウントを無効化**し、マネージド ID / RBAC でアクセスする
- **Private Endpoint** でパブリックアクセスを無効化する
- **Defender for Containers** でイメージの脆弱性スキャンを有効にする
- **Geo-Replication** でリージョン間に複製して Pull のレイテンシを削減する

```yaml
# GitHub Actions でのイメージビルド & プッシュ
- name: Build and Push Container Image
  run: |
    az acr build \
      --registry myacr \
      --image myapp:${{ github.sha }} \
      --image myapp:latest \
      .
```

### イメージタグ戦略
```
myacr.azurecr.io/myapp:latest          # 最新（本番では使用しない）
myacr.azurecr.io/myapp:v1.2.3          # セマンティックバージョン
myacr.azurecr.io/myapp:abc1234         # Git SHA（推奨：ビルドを追跡可能）
myacr.azurecr.io/myapp:main-20240301   # ブランチ-日付
```

---

## ブランチ戦略

### 推奨: GitHub Flow (シンプル)
```
main (本番デプロイ可能な状態を常に維持)
├── feature/add-login    → PR → main → 本番デプロイ
├── fix/auth-bug         → PR → main → 本番デプロイ
└── hotfix/critical-fix  → PR → main → 本番デプロイ
```

### 大規模チーム向け: Git Flow
```
main (本番)
├── develop (統合ブランチ)
│   ├── feature/xxx  → PR → develop
│   └── feature/yyy  → PR → develop
└── release/v1.2.0   → develop → main → Tag
    hotfix/xxx        → main → Tag
```

### ブランチ保護ルール (必須設定)
- `main` への直接プッシュを禁止
- PR に最低1人のレビュアー承認を必須にする
- CI (テスト・ビルド) が成功しないとマージできないようにする
- Force Push を禁止する
