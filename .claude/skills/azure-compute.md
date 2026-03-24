# Azure コンピュート ベストプラクティス

## サービス選択ガイド

```
コンテナを使う？
├── Yes → オーケストレーションが必要？
│   ├── Yes (大規模・複雑) → Azure Kubernetes Service (AKS)
│   └── No (シンプル・サーバーレス) → Azure Container Apps
└── No → マネージドランタイムで十分？
    ├── Yes → イベントドリブン・短時間処理？
    │   ├── Yes → Azure Functions
    │   └── No → Azure App Service
    └── No (OS レベルの制御が必要) → Azure Virtual Machines
```

---

## Azure Kubernetes Service (AKS)

### クラスター設計

#### ノードプール設計
```
System Node Pool (必須・管理コンポーネント用)
├── VM サイズ: Standard_D4s_v3 以上
├── ノード数: 最低3 (AZ 分散)
└── Taint: CriticalAddonsOnly=true:NoSchedule

User Node Pool (アプリワークロード用)
├── VM サイズ: ワークロードに応じて選択
├── Auto-scale: min=1, max=10
└── AZ 分散: zones = [1, 2, 3]

GPU Node Pool (AI/ML ワークロード用・必要な場合)
└── VM サイズ: Standard_NC系
```

#### 推奨設定
```bicep
resource aksCluster 'Microsoft.ContainerService/managedClusters@2023-05-01' = {
  name: 'aks-myapp-prod'
  properties: {
    // Entra ID 統合認証
    aadProfile: {
      managed: true
      enableAzureRBAC: true
    }
    // RBAC 有効化
    enableRBAC: true
    // Network Policy (Calico または Azure)
    networkProfile: {
      networkPlugin: 'azure'
      networkPolicy: 'calico'
      loadBalancerSku: 'standard'
    }
    // Azure Monitor 統合
    addonProfiles: {
      omsagent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceID: logAnalyticsWorkspace.id
        }
      }
      azureKeyvaultSecretsProvider: {
        enabled: true  // Key Vault からシークレットを Pod にマウント
      }
    }
    // プライベートクラスター (API サーバーを非公開)
    apiServerAccessProfile: {
      enablePrivateCluster: true
    }
  }
}
```

### セキュリティベストプラクティス

#### Pod セキュリティ
- `securityContext` で非 root ユーザーで実行する
- `readOnlyRootFilesystem: true` を設定する
- 不要な Linux Capability を drop する
- Pod Security Standards (Restricted) を適用する

```yaml
# Pod セキュリティコンテキストのベストプラクティス
spec:
  securityContext:
    runAsNonRoot: true
    runAsUser: 1000
    fsGroup: 2000
    seccompProfile:
      type: RuntimeDefault
  containers:
  - name: app
    securityContext:
      allowPrivilegeEscalation: false
      readOnlyRootFilesystem: true
      capabilities:
        drop: ["ALL"]
    resources:
      requests:
        cpu: "100m"
        memory: "128Mi"
      limits:
        cpu: "500m"
        memory: "512Mi"
```

#### イメージセキュリティ
- Azure Container Registry (ACR) を使用し、プライベートレジストリから Pull する
- Microsoft Defender for Containers でイメージの脆弱性スキャンを有効にする
- `latest` タグを使わず、ダイジェスト (SHA) またはセマンティックバージョンを使う
- Azure Policy でホワイトリスト以外のレジストリからの Pull を拒否する

#### ネットワークポリシー
```yaml
# デフォルト: 全 Pod 間通信を拒否
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: default-deny-all
spec:
  podSelector: {}
  policyTypes:
  - Ingress
  - Egress
```

### スケーリング
- **HPA** (Horizontal Pod Autoscaler): CPU/メモリ使用率に基づくスケール
- **KEDA**: Event-driven オートスケール（Service Bus キュー長など）
- **Cluster Autoscaler**: ノード数の自動増減
- **Spot Instances**: コスト削減（バッチジョブ・中断耐性ワークロード向け）

---

## Azure App Service

### プランの選択
| プラン | 用途 | 特徴 |
|---|---|---|
| Free/Shared | 開発・テスト | SLA なし、スケール不可 |
| Basic | 小規模本番 | カスタムドメイン、SSL |
| Standard | 本番推奨 | AutoScale、Deployment Slots |
| Premium v3 | 高パフォーマンス | VNet 統合、より大きな VM |
| Isolated v2 | コンプライアンス | 専用環境 (App Service Environment) |

### ベストプラクティス
- **Deployment Slots** で Blue/Green デプロイを実現する（本番に影響なく検証）
- **Always On** を有効にしてコールドスタートを防ぐ
- **Health Check** を設定して不健全なインスタンスを自動除外する
- **Managed Identity** でシークレットレスな認証を実現する
- VNet 統合でプライベートリソースへのアクセスを制御する

```bicep
resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: 'app-myapp-prod'
  properties: {
    httpsOnly: true  // HTTPS 強制
    siteConfig: {
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'  // FTP 無効化
      http20Enabled: true
      healthCheckPath: '/health'
      // マネージド ID でシークレットを App Configuration/Key Vault から取得
    }
    virtualNetworkSubnetId: appSubnet.id  // VNet 統合
  }
  identity: {
    type: 'SystemAssigned'
  }
}
```

---

## Azure Functions

### トリガーの選択
| トリガー | 用途 |
|---|---|
| HTTP | REST API、Webhook |
| Timer | スケジュール実行 |
| Service Bus | メッセージング、非同期処理 |
| Event Grid | イベント駆動処理 |
| Blob Storage | ファイルアップロード時の処理 |
| Cosmos DB | Change Feed 処理 |

### ホスティングプランの選択
| プラン | 特徴 | 適用場面 |
|---|---|---|
| Consumption | 実行時のみ課金、自動スケール | 散発的なワークロード |
| Premium | 常時ウォーム、VNet 統合 | 低レイテンシ、VNet 接続が必要 |
| Dedicated (App Service) | 専用 VM、コスト予測可能 | 継続的な高負荷 |
| Container Apps | コンテナ化 Functions | Kubernetes 不要のコンテナ |

### ベストプラクティス
- **Durable Functions** で長時間・ステートフルなワークフローを実装する
- Consumption プランでは**コールドスタート**に注意（重要な処理には Premium を検討）
- べき等性 (Idempotency) を確保する（リトライ時に重複処理しない）
- **Dead Letter Queue** を設定してメッセージの消失を防ぐ
- Function の実行時間は最大タイムアウト以内に収める（Consumption: 10分）

---

## Azure Virtual Machines

### VM 選択のポイント
| シリーズ | 用途 |
|---|---|
| B-series | 低コスト・バースト可能（開発/テスト） |
| D-series (Dsv5) | 汎用・バランス型（Web/アプリサーバー） |
| E-series (Esv5) | メモリ最適化（データベース、キャッシュ） |
| F-series (Fsv2) | コンピュート最適化（バッチ処理） |
| L-series | ストレージ最適化（NoSQL、ビッグデータ） |
| N-series (NV/NC) | GPU（AI/ML、グラフィックス） |

### ベストプラクティス
- 本番 VM は **Premium SSD** または **Ultra Disk** を使用する
- **Availability Sets** または **Availability Zones** で冗長化する
- **Azure Bastion** 経由で SSH/RDP し、パブリック IP を不要にする
- Just-in-Time (JIT) VM Access で管理ポートを平常時は閉じる
- Azure Update Manager で OS パッチを自動適用する
- VM Backup (Azure Backup) を設定してデータを保護する

### VM Scale Sets (VMSS)
```bicep
resource vmss 'Microsoft.Compute/virtualMachineScaleSets@2022-11-01' = {
  properties: {
    orchestrationMode: 'Flexible'  // 柔軟モードを推奨
    scaleInPolicy: {
      rules: ['OldestVM']  // 古い VM から削除
    }
    automaticRepairsPolicy: {
      enabled: true  // 不健全インスタンスの自動修復
      gracePeriod: 'PT30M'
    }
  }
}
```

---

## Azure Container Apps

### AKS vs Container Apps の使い分け
| 観点 | AKS | Container Apps |
|---|---|---|
| 制御レベル | 高（Kubernetes フル制御） | 低（マネージド） |
| 運用オーバーヘッド | 高 | 低 |
| スケーリング | HPA/KEDA | 組み込み（KEDA ベース） |
| コスト | 高（常時稼働クラスター） | 低（実行分のみ） |
| 適用場面 | 複雑なマイクロサービス | シンプルなコンテナアプリ |

### KEDA スケーリング設定例
```yaml
# Service Bus メッセージ数に基づくスケーリング
scale:
  minReplicas: 0
  maxReplicas: 20
  rules:
  - name: servicebus-scaler
    custom:
      type: azure-servicebus
      metadata:
        queueName: myqueue
        messageCount: "10"  # キュー内10メッセージにつき1レプリカ
```
