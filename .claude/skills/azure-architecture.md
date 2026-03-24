# Azure アーキテクチャ ベストプラクティス

## Azure Well-Architected Framework (WAF)

Microsoft が定義する5つの柱に基づいてシステムを設計・評価する。

### 1. 信頼性 (Reliability)
- **目標**: 障害から回復し、可用性を維持する
- SLA / SLO / SLI を明確に定義する
- 単一障害点 (SPOF) を排除し、冗長構成を取る
- リージョンペア (例: Japan East ↔ Japan West) を活用する
- Azure Availability Zones (AZ) を使って同一リージョン内で物理分離する
- 自動フェイルオーバーを実装する（Traffic Manager / Azure Front Door）
- Chaos Engineering でレジリエンスを継続的に検証する

### 2. セキュリティ (Security)
- **目標**: 脅威から資産を守る
- Zero Trust モデルを採用する（"Never trust, always verify"）
- 最小権限の原則 (Principle of Least Privilege) を徹底する
- 防御の多層化 (Defense in Depth) を実装する
- Microsoft Defender for Cloud でセキュリティスコアを継続的に監視する

### 3. コスト最適化 (Cost Optimization)
- **目標**: 無駄なコストを削減し価値を最大化する
- Azure Cost Management + Billing でコストを可視化する
- Reserved Instances / Savings Plans で長期利用コストを削減する
- 未使用リソースの定期的な棚卸しと削除
- 開発/テスト環境では自動シャットダウンを設定する

### 4. オペレーショナルエクセレンス (Operational Excellence)
- **目標**: 運用プロセスと手順を継続的に改善する
- Infrastructure as Code (IaC) でインフラをコード管理する
- CI/CD パイプラインで自動デプロイを実現する
- Azure Monitor + Log Analytics で一元的な監視を行う
- Runbook / Playbook を整備し、障害対応を標準化する

### 5. パフォーマンス効率 (Performance Efficiency)
- **目標**: リソースを効率的に使用しスケールに対応する
- 水平スケーリング (Scale Out) を優先し、垂直スケーリング (Scale Up) は補助的に使う
- Azure CDN / Front Door でコンテンツをエッジにキャッシュする
- 非同期処理 (Azure Service Bus / Event Hubs) でスループットを向上させる
- パフォーマンステストを CI/CD パイプラインに組み込む

---

## リソース設計の原則

### 命名規則
一貫した命名規則を採用することでリソースを管理しやすくする。

```
<リソースタイプ>-<アプリ名>-<環境>-<リージョン>-<連番>
例: rg-myapp-prod-japaneast-001
    vnet-myapp-prod-je-001
    aks-myapp-prod-je-001
```

| リソース | プレフィックス例 |
|---|---|
| Resource Group | rg- |
| Virtual Network | vnet- |
| Subnet | snet- |
| Network Security Group | nsg- |
| Storage Account | st (最大24文字、英数小文字のみ) |
| Key Vault | kv- |
| App Service | app- |
| AKS Cluster | aks- |
| Azure SQL | sql- |
| Cosmos DB | cosmos- |

### タグ管理
すべてのリソースに必須タグを設定し、コスト管理・運用を容易にする。

```json
{
  "Environment": "production",
  "Application": "TeamManagementTool",
  "Owner": "platform-team",
  "CostCenter": "CC-1234",
  "ManagedBy": "terraform"
}
```

Azure Policy で必須タグが存在しない場合にデプロイを拒否するポリシーを設定する。

### リソースグループの設計
- **ライフサイクル単位**でグループ化する（一緒に削除するものをまとめる）
- アプリケーション、環境、リージョンごとに分割する
- 共有インフラ（VNet, Key Vault）は専用の共有 RG に配置する

```
rg-shared-prod-japaneast        # 共有インフラ (VNet, Key Vault 等)
rg-app-backend-prod-japaneast   # バックエンドアプリ
rg-app-frontend-prod-japaneast  # フロントエンドアプリ
rg-monitoring-prod-japaneast    # 監視リソース
```

---

## 設計パターン

### マルチリージョン Active-Active
```
Internet
    ↓
Azure Front Door (グローバルロードバランシング + WAF)
    ├── Japan East (Primary)
    │   ├── App Service / AKS
    │   └── Azure SQL (Primary)
    └── Japan West (Secondary)
        ├── App Service / AKS
        └── Azure SQL (Geo-Replica)
```

### ハブ&スポーク ネットワーク
```
Hub VNet (共有インフラ)
├── Azure Firewall
├── VPN Gateway / ExpressRoute
├── Bastion
└── Spoke VNet (各アプリ) ← VNet Peering
    ├── Spoke: Production
    ├── Spoke: Staging
    └── Spoke: Development
```

### 12-Factor App on Azure
| Factor | Azure サービス |
|---|---|
| Codebase | Azure Repos / GitHub |
| Dependencies | Container Registry (ACR) |
| Config | App Configuration / Key Vault |
| Backing Services | Azure SQL, Redis Cache, Service Bus |
| Build/Release/Run | Azure DevOps / GitHub Actions |
| Processes | App Service, AKS |
| Port Binding | App Service, Container Apps |
| Concurrency | VMSS, AKS HPA |
| Disposability | Container Apps, AKS |
| Dev/Prod Parity | ARM/Bicep, Terraform |
| Logs | Azure Monitor, Log Analytics |
| Admin Processes | Azure Functions, Azure Automation |

---

## ランディングゾーン (Landing Zone)

Azure Landing Zone は、本番環境を安全・効率的に運用するための事前構成済みの基盤。

### 主要コンポーネント
1. **管理グループ (Management Groups)**: ポリシーと RBAC を階層的に適用
2. **サブスクリプション**: 環境（本番/開発）やチームで分離
3. **Azure Policy**: コンプライアンスルールを自動強制
4. **RBAC**: 最小権限でアクセス制御
5. **Hub VNet**: セントラルなネットワーク管理

### 管理グループ階層例
```
Root Management Group
└── Contoso (テナントルート)
    ├── Platform
    │   ├── Management (Log Analytics, Automation)
    │   ├── Connectivity (Hub VNet, Firewall)
    │   └── Identity (AD DS, ADFS)
    └── Landing Zones
        ├── Corp (オンプレ接続あり)
        │   ├── Production
        │   └── Development
        └── Online (インターネット公開)
            ├── Production
            └── Development
```
