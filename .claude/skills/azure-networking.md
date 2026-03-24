# Azure ネットワーキング ベストプラクティス

## Virtual Network (VNet) 設計

### アドレス空間の計画
- VNet の CIDR は将来の拡張を考慮して**広めに**取る
- オンプレミスや他の VNet と**重複しない**アドレス空間を使用する
- Azure 予約アドレス (5個/サブネット) を考慮してサブネットサイズを決める

```
推奨アドレス空間例:
Production VNet:   10.0.0.0/16  (65,536 アドレス)
Development VNet:  10.1.0.0/16
Staging VNet:      10.2.0.0/16
Hub VNet:          10.100.0.0/16
```

### サブネット分割の原則
役割・セキュリティ要件ごとにサブネットを分ける。

```
VNet: 10.0.0.0/16
├── snet-gateway:     10.0.0.0/27   (VPN/ExpressRoute Gateway 専用)
├── snet-firewall:    10.0.0.32/26  (Azure Firewall 専用: /26 必須)
├── snet-bastion:     10.0.0.128/26 (Azure Bastion 専用: /26 推奨)
├── snet-appgw:       10.0.1.0/24   (Application Gateway)
├── snet-web:         10.0.2.0/24   (Web フロントエンド)
├── snet-app:         10.0.3.0/24   (アプリケーション層)
├── snet-data:        10.0.4.0/24   (データベース層)
└── snet-private-ep:  10.0.5.0/24   (Private Endpoint 専用)
```

---

## Network Security Group (NSG)

### 設計原則
- すべてのサブネットに NSG を適用する（デフォルト Deny）
- 必要最小限のポートのみ許可する
- NSG フローログを有効にして Traffic Analytics で可視化する
- タグ (ServiceTag) を使って Azure サービスへのアクセスを管理する

### 推奨 NSG ルール例（アプリケーション層）
```
優先度  方向   プロトコル  ソース              宛先          ポート  アクション
100    Inbound  TCP       snet-web (10.0.2.0/24) *           8080   Allow
200    Inbound  TCP       AzureLoadBalancer      *           *      Allow
300    Inbound  *         VirtualNetwork         *           *      Deny  (デフォルトルールより前にブロック)
65000  Inbound  *         VirtualNetwork         VirtualNetwork *   Allow (デフォルト)
65001  Inbound  *         AzureLoadBalancer      *           *      Allow (デフォルト)
65500  Inbound  *         *                      *           *      Deny  (デフォルト)
```

### Application Security Group (ASG)
IP アドレスではなく**アプリケーションの役割**でグループ化する。

```bicep
// Web サーバーグループ
resource asgWeb 'Microsoft.Network/applicationSecurityGroups@2022-07-01' = {
  name: 'asg-web'
  location: location
}

// NSG ルール: Web → App 通信のみ許可
{
  name: 'Allow-Web-To-App'
  properties: {
    priority: 100
    protocol: 'Tcp'
    sourceApplicationSecurityGroups: [{ id: asgWeb.id }]
    destinationApplicationSecurityGroups: [{ id: asgApp.id }]
    destinationPortRange: '8080'
    access: 'Allow'
    direction: 'Inbound'
  }
}
```

---

## Azure Firewall

### 用途
- 東西トラフィック（VNet 間、オンプレミス↔Azure）を検査・制御する
- アウトバウンドインターネット通信を FQDN レベルで制御する
- Hub & Spoke トポロジで Hub VNet に配置し、中央集権的に管理する

### ルールの種類
| ルール種別 | 用途 | 例 |
|---|---|---|
| DNAT Rules | インバウンド NAT | Internet → FW (443) → Internal Server |
| Network Rules | IP/ポートベースのL4制御 | AKS → Azure Monitor (443) |
| Application Rules | FQDN ベースの L7 制御 | *.microsoft.com, pypi.org |

### Azure Firewall vs NSG
| 機能 | NSG | Azure Firewall |
|---|---|---|
| L4 フィルタリング | ✓ | ✓ |
| L7 フィルタリング | ✗ | ✓ (FQDN, HTTP/S) |
| 脅威インテリジェンス | ✗ | ✓ |
| ログ集中管理 | フローログのみ | ✓ (中央集権) |
| 価格 | 無料 | 高コスト |

**推奨**: NSG でマイクロセグメンテーション + Azure Firewall でサブネット間・アウトバウンド制御を組み合わせる。

---

## Azure Front Door & CDN

### Azure Front Door (Premium 推奨)
グローバルアプリケーションの高可用性・パフォーマンス向上に使用する。

**主な機能:**
- グローバルロードバランシング（最も近い PoP にルーティング）
- SSL オフロード・HTTP→HTTPS リダイレクト
- WAF（Web Application Firewall）の組み込み
- Origin へのプライベート接続（Private Link 対応）
- キャッシュ・圧縮による配信最適化

```bicep
resource frontDoor 'Microsoft.Cdn/profiles@2022-11-01-preview' = {
  name: 'afd-myapp-prod'
  location: 'global'
  sku: {
    name: 'Premium_AzureFrontDoor'  // WAF + Private Link 対応
  }
}
```

### Application Gateway vs Front Door
| 機能 | Application Gateway | Azure Front Door |
|---|---|---|
| スコープ | リージョン内 | グローバル (マルチリージョン) |
| L7 ロードバランシング | ✓ | ✓ |
| WAF | ✓ | ✓ |
| SSL 終端 | ✓ | ✓ |
| マルチリージョン | ✗ | ✓ |
| CDN | ✗ | ✓ |
| WebSocket | ✓ | ✗ |

**使い分け:**
- 単一リージョンの内部アプリ → Application Gateway
- グローバル展開・CDN が必要 → Azure Front Door

---

## Private Endpoint / Private Link

### なぜ使うか
Azure PaaS サービス（Storage, SQL, Key Vault 等）をパブリックインターネットに公開せず、VNet 内のプライベート IP で接続する。

### 設計パターン
```
VNet (snet-private-ep)
└── Private Endpoint (10.0.5.4) → Azure Storage Account
                                → Azure SQL
                                → Azure Key Vault
                                → Azure Container Registry
```

### 実装手順
1. Private Endpoint を作成（VNet/サブネットに配置）
2. ターゲットサービスの**パブリックアクセスを無効化**
3. **Private DNS Zone** を VNet にリンクし、FQDN を解決できるようにする

```bicep
// Private DNS Zone for Storage Blob
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.blob.core.windows.net'
  location: 'global'
}

// VNet にリンク
resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: privateDnsZone
  name: 'link-to-vnet'
  location: 'global'
  properties: {
    virtualNetwork: { id: vnet.id }
    registrationEnabled: false
  }
}
```

---

## DNS 設計

### Azure Private DNS Zones
- PaaS サービスの Private Endpoint ごとに Private DNS Zone を作成する
- Hub VNet に DNS Resolver を配置し、すべてのスポークから集中解決する

### 主な Private DNS Zone
| サービス | DNS Zone |
|---|---|
| Azure Blob Storage | privatelink.blob.core.windows.net |
| Azure SQL Database | privatelink.database.windows.net |
| Azure Key Vault | privatelink.vaultcore.azure.net |
| Azure Container Registry | privatelink.azurecr.io |
| Azure Kubernetes Service | privatelink.{region}.azmk8s.io |
| Azure Cosmos DB | privatelink.documents.azure.com |

---

## VPN / ExpressRoute

### 選択基準
| 要件 | 推奨 |
|---|---|
| 開発・小規模接続 | Site-to-Site VPN |
| 本番・高帯域・低遅延 | ExpressRoute |
| 外出先からの接続 | Point-to-Site VPN |
| 高可用性 (SLA 99.95%) | ExpressRoute + VPN フォールバック |

### ExpressRoute ベストプラクティス
- 冗長回線（プロバイダー2社または2回線）を使用する
- Global Reach でリージョン間を直接接続する
- ExpressRoute Direct は 10/100Gbps の大帯域が必要な場合に使用する

---

## Load Balancer の使い分け

| サービス | レイヤー | スコープ | 用途 |
|---|---|---|---|
| Azure Load Balancer | L4 | リージョン内 | VM の内部/外部ロードバランシング |
| Application Gateway | L7 | リージョン内 | HTTP/S、パスベースルーティング、WAF |
| Azure Front Door | L7 | グローバル | マルチリージョン、CDN、WAF |
| Traffic Manager | DNS | グローバル | DNS ベースのルーティング（Front Door の補完） |
