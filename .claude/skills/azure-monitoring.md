# Azure 監視・可観測性 ベストプラクティス

## 可観測性の3本柱 (Three Pillars of Observability)

| 柱 | Azure サービス | 説明 |
|---|---|---|
| メトリクス (Metrics) | Azure Monitor Metrics | 数値データの時系列（CPU使用率、リクエスト数等） |
| ログ (Logs) | Log Analytics Workspace | 構造化・非構造化ログ、イベント |
| トレース (Traces) | Application Insights | 分散トレーシング、リクエストの追跡 |

---

## Azure Monitor アーキテクチャ

```
Azure リソース
├── VM / VMSS
├── AKS
├── App Service / Functions
├── Azure SQL
└── ...
     ↓ 診断設定 (Diagnostic Settings)
Azure Monitor
├── Metrics (リアルタイム、93日間保存)
└── Logs → Log Analytics Workspace (長期保存・分析)
              ├── Azure Workbooks (ダッシュボード)
              ├── Alerts (アラート)
              ├── Application Insights
              └── Microsoft Sentinel (SIEM)
```

### Log Analytics Workspace 設計
- **環境ごと**にワークスペースを分割する（本番・開発でログを混在させない）
- または**中央集権型**で1つのワークスペースにすべてのログを集め、コスト効率を上げる
- データ保持期間: デフォルト30日（本番は最低90日、コンプライアンス要件は1年以上）
- **テーブルレベルの保持期間設定**で高頻度テーブルの保持を短くしコストを削減する

---

## Application Insights

### セットアップ
すべての Web アプリケーション・API に Application Insights を統合する。

```csharp
// .NET アプリケーション
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// カスタムテレメトリ
public class OrderService
{
    private readonly TelemetryClient _telemetry;

    public async Task ProcessOrder(Order order)
    {
        using var operation = _telemetry.StartOperation<RequestTelemetry>("ProcessOrder");
        try
        {
            // 処理
            _telemetry.TrackEvent("OrderProcessed", new Dictionary<string, string>
            {
                ["OrderId"] = order.Id,
                ["Amount"] = order.Amount.ToString()
            });
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex);
            operation.Telemetry.Success = false;
            throw;
        }
    }
}
```

### 分散トレーシング
マイクロサービス間でリクエストを追跡する。

```
フロントエンド → [API Gateway] → [Order Service] → [Payment Service]
                                         ↓                   ↓
                                   [DB 呼び出し]       [外部 API 呼び出し]

Application Map でサービス間の依存関係とパフォーマンスを可視化
```

### 重要な監視指標
- **Application Map**: サービス間の依存関係とエラー率
- **Live Metrics**: リアルタイムのリクエスト・エラー・パフォーマンス
- **Failures**: 例外・失敗したリクエストの詳細
- **Performance**: レイテンシのパーセンタイル分布 (P50/P95/P99)
- **Availability**: 外部から定期的にエンドポイントを監視 (URL Ping Test)

---

## KQL (Kusto Query Language)

Log Analytics のクエリ言語。効率的なログ分析に必須。

### 基本構文
```kql
// テーブルからデータを取得し変換する
TableName
| where TimeGenerated > ago(1h)          // 直近1時間
| where Level == "Error"                  // エラーのみ
| project TimeGenerated, Message, Level  // 必要な列のみ
| order by TimeGenerated desc            // 時刻降順
| take 100                               // 最初の100件
```

### よく使うクエリ例

```kql
// エラー率の時系列グラフ
requests
| where timestamp > ago(24h)
| summarize
    Total = count(),
    Errors = countif(success == false)
    by bin(timestamp, 5m)
| extend ErrorRate = toreal(Errors) / Total * 100
| render timechart

// レイテンシのパーセンタイル
requests
| where timestamp > ago(1h)
| summarize
    P50 = percentile(duration, 50),
    P95 = percentile(duration, 95),
    P99 = percentile(duration, 99)
    by operation_Name
| order by P99 desc

// 診断設定が未設定のリソースを検索
resources
| where type has "Microsoft.Compute"
| join kind=leftouter (
    diagnosticsettings
    | where properties.workspaceId != ""
) on id
| where isempty(id1)
| project name, type, resourceGroup
```

---

## アラート設計

### アラートの種類
| 種類 | データソース | 用途 |
|---|---|---|
| Metric Alerts | Azure Monitor Metrics | CPU使用率、レスポンスタイム等 |
| Log Alerts | Log Analytics | カスタムログクエリ結果 |
| Activity Log Alerts | Activity Log | リソースの変更、サービスヘルス |
| Smart Detection | Application Insights | 異常検知（自動） |

### アラート設計の原則
- **アクショナブル**: アラートは対応できるものだけ設定する（ノイズを増やさない）
- **重要度分類**: Critical / High / Medium / Low で優先度を明確にする
- **段階的なアラート**: 警告(Warning)と重大(Critical)の2段階で設定する
- **アラート疲れを防ぐ**: 頻繁に発火するアラートは閾値を見直す

### Action Group の設定
```bicep
resource actionGroup 'Microsoft.Insights/actionGroups@2022-06-15' = {
  name: 'ag-ops-team'
  properties: {
    groupShortName: 'OpsTeam'
    enabled: true
    emailReceivers: [
      {
        name: 'OpsTeam Email'
        emailAddress: 'ops-team@example.com'
        useCommonAlertSchema: true
      }
    ]
    webhookReceivers: [
      {
        name: 'Slack Webhook'
        serviceUri: 'https://hooks.slack.com/services/...'
        useCommonAlertSchema: true
      }
    ]
  }
}
```

### 推奨アラート例
| メトリクス | 警告閾値 | 重大閾値 |
|---|---|---|
| CPU 使用率 | 70% | 90% |
| メモリ使用率 | 80% | 95% |
| HTTP 5xx エラー率 | 1% | 5% |
| API レスポンスタイム P95 | 500ms | 1000ms |
| ディスク使用率 | 80% | 90% |
| AKS Pod 再起動回数 | 5回/時 | 10回/時 |

---

## Azure Workbooks & Dashboards

### Workbook の活用
- インタラクティブなレポートをコードで管理できる（JSON形式）
- Azure Monitor Community で公開されている Workbook を活用する
- SRE チームの運用ダッシュボード、管理職向けの可用性レポートを作成する

### 推奨ダッシュボード構成
```
Executive Dashboard (管理職向け)
├── サービス可用性 (%)
├── SLA 達成状況
└── インシデント数

Operations Dashboard (SRE/運用向け)
├── リアルタイムメトリクス (CPU, Memory, Network)
├── エラー率・レイテンシ
├── アクティブアラート
└── AKS クラスター状態

Application Dashboard (開発チーム向け)
├── Application Map
├── リクエスト数・失敗率
├── パフォーマンス (P50/P95/P99)
└── 例外一覧
```

---

## コスト最適化

### Log Analytics のコスト管理
Log Analytics のデータ取り込みコストは高くなりがちなので注意する。

- **データ収集ルール (DCR)**: 不要なデータを取り込まないようフィルタリング
- **テーブルレベルの保持期間**: アクセス頻度の低いテーブルは Basic ログ（安価）に設定
- **Commitment Tiers**: 1日100GB以上の場合はコミットメント価格が割安
- **診断ログの取捨選択**: すべてのデバッグログをフル取り込みしない

```kql
// 先月のテーブル別データ取り込み量を確認
Usage
| where TimeGenerated > ago(30d)
| summarize TotalGB = sum(Quantity) / 1000 by DataType
| order by TotalGB desc
```
