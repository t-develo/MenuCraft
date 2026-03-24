# Azure ストレージ & データベース ベストプラクティス

## データベース選択ガイド

```
データ構造は？
├── リレーショナル (SQL)
│   ├── マネージドが必要？ → Azure SQL Database / Managed Instance
│   ├── オープンソース DB？
│   │   ├── MySQL → Azure Database for MySQL
│   │   └── PostgreSQL → Azure Database for PostgreSQL
│   └── オンプレミス互換性が最優先 → SQL Server on VM
└── 非リレーショナル (NoSQL)
    ├── ドキュメント → Cosmos DB (Core SQL API)
    ├── キー・バリュー → Cosmos DB / Azure Cache for Redis
    ├── グラフ → Cosmos DB (Gremlin API)
    ├── 時系列 → Azure Data Explorer / InfluxDB on VM
    └── テーブル → Cosmos DB (Table API) / Azure Table Storage
```

---

## Azure SQL Database

### サービス階層の選択
| 階層 | 用途 | 特徴 |
|---|---|---|
| General Purpose | 標準ワークロード | バランス型、99.99% SLA |
| Business Critical | 高負荷・低レイテンシ | 高IOPS、インメモリ OLTP、読み取りレプリカ付き |
| Hyperscale | 大容量（100TB+） | 高速スケール、分散アーキテクチャ |

**Serverless** (General Purpose): 断続的な使用に最適。使用していない時間は自動一時停止でコスト削減。

### 高可用性設計
```
Business Critical 階層:
Primary (書き込み + 読み取り)
├── Secondary Replica 1 (読み取り専用)
├── Secondary Replica 2 (読み取り専用)
└── Secondary Replica 3 (フェイルオーバー待機)

Geo-Replication / Failover Groups:
Japan East (Primary) ←→ Japan West (Secondary)
- 自動フェイルオーバー設定で RTO を最小化
- アプリは Failover Group エンドポイント (1つの FQDN) を使用
```

### ベストプラクティス
- **Always Encrypted** で機密データを列レベルで暗号化する（Key Vault で鍵管理）
- **Transparent Data Encryption (TDE)** はデフォルト有効（at rest 暗号化）
- **Azure Defender for SQL** を有効にして SQL インジェクション等を検知する
- **Private Endpoint** でパブリックアクセスを無効化する
- **接続文字列にパスワードを使わない**: マネージド ID + AAD 認証を使用する

```csharp
// マネージド ID で Azure SQL に接続（パスワード不要）
var connectionString = "Server=tcp:myserver.database.windows.net;Database=mydb;Authentication=Active Directory Managed Identity;";
```

### バックアップと復旧
| 機能 | 説明 |
|---|---|
| 自動バックアップ | フル: 週1回、差分: 12時間、ログ: 5-10分 |
| PITR (Point-in-Time Restore) | 1〜35日以内の任意の時点に復元 |
| Long-term Retention (LTR) | 最大10年間のバックアップ保持 |
| Geo-Redundant Backup | ペアリージョンに自動コピー |

---

## Azure Cosmos DB

### API の選択
| API | 用途 | 移行元 |
|---|---|---|
| NoSQL (Core) | ドキュメント DB（推奨デフォルト） | 新規開発 |
| MongoDB | MongoDB 互換 | 既存 MongoDB |
| Cassandra | 列指向 | 既存 Cassandra |
| Gremlin | グラフ DB | グラフ処理 |
| Table | キー・バリュー | Azure Table Storage 移行 |
| PostgreSQL | 分散 PostgreSQL (Citus) | 大規模 PostgreSQL |

### パーティションキー設計
パーティションキーはパフォーマンスの最重要項目。

**良いパーティションキー:**
- カーディナリティが高い（ユーザーID、テナントID など）
- 読み書きが均等に分散される
- トランザクション境界（同一パーティション内でトランザクション処理）

**悪いパーティションキー:**
- カーディナリティが低い（true/false、地域名）
- ホットパーティションが発生するキー

```json
// 良い例: ユーザーIDをパーティションキーに
{
  "id": "order-001",
  "userId": "user-12345",  // パーティションキー
  "items": [...],
  "totalAmount": 5000
}
```

### 整合性レベルの選択
| レベル | 遅延 | 強さ | 用途 |
|---|---|---|---|
| Strong | 高 | 最強 | 金融トランザクション |
| Bounded Staleness | 中〜高 | 強 | 最大N操作または T時間の遅れを許容 |
| Session | 低 | 中 | デフォルト推奨（自分の書き込みは即読める）|
| Consistent Prefix | 低 | 中 | 順序は保証、遅延は許容 |
| Eventual | 最低 | 最弱 | ソーシャルフィード、カウンター |

**推奨: Session 整合性**（大多数のアプリで適切なバランス）

### コスト最適化
- **オートスケール** を使って RU/s を需要に応じて調整する
- **マルチリージョン書き込み**は必要な場合のみ（コストが倍増）
- **分析ストア** (Synapse Link) でオンライン分析クエリを OLTP と分離する
- TTL (Time to Live) で古いデータを自動削除する

---

## Azure Storage (Blob / Files / Queue / Table)

### ストレージアカウントの種類
| 種類 | 用途 |
|---|---|
| Standard (GPv2) | 汎用（Blob, Files, Queue, Table） |
| Premium Block Blobs | 高 IOPS Blob（低レイテンシが必要な場合）|
| Premium File Shares | 高パフォーマンス Azure Files |
| Premium Page Blobs | VM ディスク (非マネージドディスクは非推奨) |

### Blob ストレージのアクセス層
| 層 | 保存コスト | アクセスコスト | 用途 |
|---|---|---|---|
| Hot | 高 | 低 | 頻繁にアクセスされるデータ |
| Cool | 中 | 中 | 30日以上アクセスされないデータ |
| Cold | 低 | 高 | 90日以上アクセスされないデータ |
| Archive | 最低 | 最高 + 復元時間 | 長期保存（1年以上）|

**ライフサイクル管理ポリシー**で自動的に層を移動しコストを最適化する:
```json
{
  "rules": [{
    "name": "move-to-cool-after-30-days",
    "type": "Lifecycle",
    "definition": {
      "filters": { "blobTypes": ["blockBlob"] },
      "actions": {
        "baseBlob": {
          "tierToCool": { "daysAfterModificationGreaterThan": 30 },
          "tierToArchive": { "daysAfterModificationGreaterThan": 365 },
          "delete": { "daysAfterModificationGreaterThan": 2555 }
        }
      }
    }
  }]
}
```

### セキュリティベストプラクティス
- **共有アクセスキー (Storage Account Key)** の直接使用を避ける
  - マネージド ID + RBAC を使用する
  - やむを得ず SAS (Shared Access Signature) を使う場合は短期間・最小スコープで発行
- **パブリックアクセスを無効化**し、Private Endpoint を使用する
- **Soft Delete** を有効にする（Blob: 7日以上、Container: 7日以上）
- **Immutable Storage** で WORM (Write Once Read Many) を実現する（コンプライアンス要件）
- 診断ログを Log Analytics に送信する

```bicep
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false     // パブリックアクセス無効
    allowSharedKeyAccess: false      // 共有キーアクセス無効 (マネージドIDを強制)
    supportsHttpsTrafficOnly: true
    publicNetworkAccess: 'Disabled'
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    }
  }
}
```

### レプリケーションの選択
| オプション | 冗長性 | 用途 |
|---|---|---|
| LRS (Locally Redundant) | 同一データセンター内3コピー | 開発・テスト |
| ZRS (Zone Redundant) | 同一リージョン3AZ | 本番推奨（リージョン内冗長）|
| GRS (Geo Redundant) | ペアリージョンに LRS | 災害復旧 |
| GZRS (Geo-Zone Redundant) | ZRS + ペアリージョン LRS | 最高可用性・本番 |

---

## Azure Cache for Redis

### 活用パターン
1. **セッションキャッシュ**: Web アプリのセッションデータを分散キャッシュに格納
2. **データキャッシュ**: DB クエリ結果のキャッシュ（Cache-Aside パターン）
3. **メッセージブローカー**: Pub/Sub（軽量な場合のみ。本格的には Service Bus を使用）
4. **レート制限**: API リクエスト数の制限

### キャッシュ戦略
```
Cache-Aside パターン:
1. アプリがキャッシュを確認
2. キャッシュミス → DB からデータ取得
3. キャッシュにデータを書き込む
4. 次回以降はキャッシュから返す
```

### ベストプラクティス
- **TTL (有効期限)** を必ず設定してキャッシュが古くならないようにする
- **connection pooling** を使用して接続を再利用する
- **Geo-Replication** (Premium 以上) でマルチリージョン対応する
- Private Endpoint でセキュアに接続する
- `maxmemory-policy` を適切に設定する（`allkeys-lru` が多くの場合に適切）

---

## Azure Service Bus / Event Hubs / Event Grid

### メッセージングサービスの使い分け
| サービス | メッセージタイプ | 用途 |
|---|---|---|
| Service Bus | コマンド (Command) | マイクロサービス間の信頼性の高いメッセージング、注文処理 |
| Event Hubs | イベントストリーム | ビッグデータパイプライン、ログ収集、テレメトリ |
| Event Grid | イベント通知 | Azure リソースの変更通知、Webhook トリガー |

### Service Bus ベストプラクティス
- **Dead Letter Queue (DLQ)** を監視して処理失敗メッセージを救済する
- **メッセージロック** の有効期限はメッセージ処理時間より長く設定する
- **トランザクション** でアトミックな送受信を実現する
- コンシューマーはべき等処理を実装する（同じメッセージが複数回届いてもOK）
