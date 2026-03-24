# Azure コスト管理 ベストプラクティス

## コスト管理の基本サイクル

```
可視化 (Visibility)
    → 分析 (Analysis)
    → 最適化 (Optimization)
    → 予算管理 (Governance)
    → 可視化 (繰り返し)
```

---

## コスト可視化

### Azure Cost Management + Billing
- **コスト分析**: リソース・サブスクリプション・タグ別にコストを分析する
- **コストアラート**: 予算超過前にアラートを受け取る
- **推奨事項**: Azure Advisor のコスト削減推奨事項を定期確認する

### タグによるコスト配賦
```bash
# 全リソースにコストセンタータグを設定する（Azure Policy で強制）
az policy assignment create \
  --name "require-cost-center-tag" \
  --policy "/providers/Microsoft.Authorization/policyDefinitions/..." \
  --scope "/subscriptions/{subscriptionId}"
```

### 予算アラートの設定
```bicep
resource budget 'Microsoft.Consumption/budgets@2021-10-01' = {
  name: 'monthly-budget-prod'
  properties: {
    category: 'Cost'
    amount: 100000  // 月額上限 (円換算で設定)
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: '2024-01-01'
    }
    notifications: {
      actual80Percent: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 80
        contactEmails: ['finance@example.com', 'ops@example.com']
      }
      forecasted100Percent: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 100
        contactEmails: ['finance@example.com']
      }
    }
  }
}
```

---

## コスト削減戦略

### 1. Reserved Instances / Savings Plans

長期利用が確定しているリソースには前払いで大幅割引を受ける。

| オプション | 割引率 | 柔軟性 | 対象 |
|---|---|---|---|
| 1年 Reserved Instance | ~30-40% | 中 | VM、SQL、AKS ノード等 |
| 3年 Reserved Instance | ~50-60% | 低 | 長期安定ワークロード |
| Savings Plans (1年) | ~15% | 高（コンピュート全般） | 変動する VM ワークロード |
| Savings Plans (3年) | ~25% | 高 | 長期コンピュートコミット |

**推奨判断基準**:
- 使用率が**常に 70%以上**のリソース → Reserved Instance を検討
- 使用パターンが不規則 → Savings Plans を検討
- 使用期間が不明 → PAYG (従量課金) のまま

### 2. Spot / Spot VM の活用

中断可能なワークロードに最大 90% 割引で VM を使用する。

**適用場面:**
- バッチ処理（画像変換、レポート生成）
- CI/CD ランナー
- AKS の非本番ノードプール
- Machine Learning トレーニング

```bicep
// AKS Spot ノードプール
resource spotNodePool 'Microsoft.ContainerService/managedClusters/agentPools@2023-05-01' = {
  name: 'spotpool'
  properties: {
    scaleSetPriority: 'Spot'              // Spot 優先
    scaleSetEvictionPolicy: 'Delete'     // 退避時に削除
    spotMaxPrice: -1                      // 市場価格の上限なし
    nodeTaints: ['kubernetes.azure.com/scalesetpriority=spot:NoSchedule']
    enableAutoScaling: true
    minCount: 0
    maxCount: 20
  }
}
```

### 3. 自動シャットダウン（開発/テスト環境）

開発環境の VM は業務時間外に自動停止する。

```bicep
// VM の自動シャットダウン設定 (JST 20:00)
resource autoShutdown 'Microsoft.DevTestLab/schedules@2018-09-15' = {
  name: 'shutdown-computevm-${vmName}'
  location: location
  properties: {
    status: 'Enabled'
    taskType: 'ComputeVmShutdownTask'
    dailyRecurrence: {
      time: '1100'  // UTC 11:00 = JST 20:00
    }
    timeZoneId: 'Tokyo Standard Time'
    targetResourceId: vm.id
    notificationSettings: {
      status: 'Enabled'
      timeInMinutes: 30
      emailRecipient: 'dev-team@example.com'
    }
  }
}
```

### 4. 適切なサイジング (Right-sizing)

**Azure Advisor** の推奨事項を定期確認する。

```bash
# CPU 使用率が低い VM を確認 (KQL)
# Metrics で過去30日間の最大CPU使用率を確認
InsightsMetrics
| where TimeGenerated > ago(30d)
| where Name == "Processor Utilization"
| summarize MaxCPU = max(Val) by Computer
| where MaxCPU < 20  // 最大CPU 20%未満
| order by MaxCPU asc
```

**ダウンサイズの目安:**
- CPU 使用率が常時 20%以下 → 1段階小さい SKU を検討
- CPU 使用率が常時 10%以下 → 2段階小さい SKU または Burstable VM を検討

### 5. ストレージコストの最適化

```
Blob ライフサイクル管理 (再掲):
- 30日後: Hot → Cool (約50% 削減)
- 365日後: Cool → Archive (約80% 削減)
- 2555日後 (7年): 削除

マネージドディスクの最適化:
- 使用されていないディスク (VM に未アタッチ) を削除
- LRS (ローカル冗長) で十分な場合は ZRS を使わない
- Premium SSD が不要な場合は Standard SSD を使用
```

### 6. ネットワーキングコストの最適化

```
データ転送コスト:
- Azure リージョン内: 無料
- リージョン間: 有料 (日本では約 $0.02/GB)
- インターネット向け: 有料

削減策:
- Azure Front Door / CDN でエッジキャッシュを活用する
- サービスと同じリージョンにデータを置く
- Private Endpoint は同一リージョン内なら転送コスト無料
- VNet Peering のデータ転送コストを考慮する
```

---

## コストガバナンス

### サブスクリプション分離によるコスト管理
```
Management Group
├── Production Subscription      → 本番コスト
├── Development Subscription     → 開発コスト
└── Shared Services Subscription → 共有インフラコスト（各プロジェクトに按分）
```

### Azure Policy でコスト制御
```json
// 高コスト VM SKU のデプロイを禁止
{
  "if": {
    "allOf": [
      { "field": "type", "equals": "Microsoft.Compute/virtualMachines" },
      {
        "field": "Microsoft.Compute/virtualMachines/sku.name",
        "in": ["Standard_M128s", "Standard_M64s"]
      }
    ]
  },
  "then": { "effect": "Deny" }
}
```

### コスト削減の優先順位
| 優先度 | アクション | 期待削減率 |
|---|---|---|
| 高 | 使用されていないリソースの削除 | 即時 100% |
| 高 | Reserved Instances の購入 | 30-60% |
| 中 | 開発環境の自動シャットダウン | 50-70%（夜間・休日）|
| 中 | 適切なサイジング | 20-50% |
| 中 | Spot VM の活用（適切なワークロード）| 60-90% |
| 低 | ストレージ階層の最適化 | 30-80% |
| 低 | CDN キャッシュ率の向上 | 10-30% |

---

## FinOps 文化の醸成

### チームへの原則
1. **コスト可視化**: エンジニアが自分のコードのコストを見えるようにする
2. **コスト効率の定義**: コストだけでなく「コスト/価値」で判断する
3. **継続的な最適化**: スプリントごとにコスト見直しをルーティン化する
4. **コスト責任の分散**: 中央チームだけでなく各チームがコストオーナーになる

### Azure Advisor の定期確認
- 週次または月次で Azure Advisor のコストタブを確認する
- 推奨事項の対応状況を追跡する
- 却下した推奨事項には理由を記録する

```bash
# Azure Advisor のコスト推奨事項を CLI で確認
az advisor recommendation list \
  --category Cost \
  --output table
```
