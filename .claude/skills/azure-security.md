# Azure セキュリティ ベストプラクティス

## Zero Trust アーキテクチャ

### 3つの原則
1. **明示的に検証する** (Verify Explicitly): 利用可能なすべてのデータポイントを使って認証・認可する
2. **最小権限アクセス** (Use Least Privilege Access): JIT/JEA でアクセスを制限し、リスクベースのアダプティブポリシーを適用する
3. **侵害を想定する** (Assume Breach): 影響範囲を最小化し、エンドツーエンドの暗号化を実施する

---

## ID とアクセス管理 (IAM)

### Microsoft Entra ID (旧 Azure AD)

#### 多要素認証 (MFA)
- すべてのユーザーアカウントで MFA を必須にする
- 特権アカウント (Global Admin 等) では FIDO2 セキュリティキーまたは Authenticator App を使用する
- Conditional Access Policy で MFA を強制する

```bicep
// Conditional Access は Entra ID P1/P2 ライセンスが必要
// ポータル: Entra ID > Protection > Conditional Access
```

#### Conditional Access Policy の推奨設定
| ポリシー | 対象 | 条件 | 制御 |
|---|---|---|---|
| MFA for All Users | 全ユーザー | すべてのアプリ | MFA 要求 |
| Block Legacy Auth | 全ユーザー | レガシー認証プロトコル | ブロック |
| Admin MFA | 管理者ロール | すべてのアプリ | MFA + 準拠デバイス |
| Risk-based MFA | 全ユーザー | サインインリスク: 中以上 | MFA 要求 |

#### Privileged Identity Management (PIM)
- 特権ロールは常時割り当てではなく、**JIT (Just-In-Time)** で一時的に有効化する
- 有効化時に承認フローと理由入力を必須にする
- 最大有効化時間は8時間以下に設定する

```
通常ユーザー → PIM でロール有効化申請 → 承認者が承認 → 一定時間のみ特権付与 → 自動失効
```

---

## RBAC (ロールベースアクセス制御)

### 設計原則
- **組み込みロール**を優先使用し、カスタムロールは必要最小限にする
- リソースレベルではなく**リソースグループ**または**サブスクリプション**レベルで割り当てる
- ユーザー個人ではなく**セキュリティグループ**にロールを割り当てる
- 定期的（四半期ごと）にアクセス権をレビューする (Access Review)

### よく使う組み込みロール
| ロール | 用途 |
|---|---|
| Owner | リソースの完全制御 + アクセス管理（最小限の使用に留める） |
| Contributor | リソースの作成・管理（アクセス管理は不可） |
| Reader | 読み取り専用 |
| User Access Administrator | アクセス管理のみ |
| AcrPull | ACR からのイメージ Pull のみ |
| Key Vault Secrets User | Key Vault シークレットの読み取りのみ |
| Storage Blob Data Reader | Blob の読み取りのみ |

### マネージド ID の活用
Azure リソース間の認証には**マネージド ID** を使用し、認証情報 (パスワード/キー) を使わない。

```bicep
// App Service にシステム割り当てマネージド ID を付与
resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceName
  identity: {
    type: 'SystemAssigned'
  }
}

// Key Vault へのアクセス権を付与
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2022-07-01' = {
  name: '${keyVaultName}/add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: appService.identity.principalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
}
```

---

## Azure Key Vault

### ベストプラクティス
- アプリケーションのシークレット・証明書・暗号化キーはすべて Key Vault で管理する
- アクセスモデルは **RBAC** を推奨（レガシーなアクセスポリシーより細かい制御が可能）
- **Soft Delete** と **Purge Protection** を必ず有効にする（誤削除対策）
- Key Vault は **Private Endpoint** 経由でのみアクセスし、パブリックアクセスを無効にする
- ログを Log Analytics に送信し、アクセス監査を行う

```bicep
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true        // RBAC モードを有効化
    enableSoftDelete: true               // Soft Delete を有効化
    softDeleteRetentionInDays: 90        // 90日間保持
    enablePurgeProtection: true          // 完全削除を防止
    publicNetworkAccess: 'Disabled'      // パブリックアクセスを無効
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    }
  }
}
```

### シークレットのローテーション
- Key Vault の**自動ローテーション**機能を有効にする
- ローテーション前に Event Grid 通知でアプリに最新バージョンを取得させる
- アプリは特定バージョンではなく**最新バージョン**を参照するようにする

---

## ネットワークセキュリティ

### Microsoft Defender for Cloud
- すべてのサブスクリプションで有効にする
- **セキュリティスコア** を定期的に確認し、推奨事項を対処する
- **Just-in-Time VM Access** を有効にして SSH/RDP ポートを通常は閉じる
- Defender for Containers で AKS のランタイム脅威を検知する

### Web Application Firewall (WAF)
- 外部公開 Web アプリには必ず WAF を配置する
- Azure Front Door または Application Gateway の WAF を使用する
- **Prevention モード** (検知だけでなくブロックも行う) に設定する
- OWASP Core Rule Set 3.2 以上を適用する

---

## データ保護

### 暗号化
| データ状態 | Azure の対応 |
|---|---|
| 保存時 (at rest) | Storage Service Encryption (AES-256) がデフォルトで有効 |
| 転送時 (in transit) | TLS 1.2 以上を強制。HTTP → HTTPS リダイレクトを設定 |
| 使用時 (in use) | Azure Confidential Computing (必要な場合) |

### Customer-Managed Keys (CMK)
- コンプライアンス要件がある場合は CMK を使用し、暗号化キーを自社管理する
- CMK は Key Vault (HSM) で管理する
- デフォルトの Microsoft Managed Keys で問題ない場合は CMK は不要（管理オーバーヘッドが増える）

### Azure Policy でセキュリティを強制
```json
// TLS 1.2 未満のストレージアカウントをデプロイ拒否するポリシー例
{
  "if": {
    "allOf": [
      {
        "field": "type",
        "equals": "Microsoft.Storage/storageAccounts"
      },
      {
        "field": "Microsoft.Storage/storageAccounts/minimumTlsVersion",
        "notEquals": "TLS1_2"
      }
    ]
  },
  "then": {
    "effect": "Deny"
  }
}
```

---

## セキュリティ監視

### Microsoft Sentinel (SIEM/SOAR)
- ログを一元的に収集し、脅威を検知・対応する
- Azure サービスのデータコネクタを有効にする
- Analytic Rules で異常なサインインやリソース操作を検知する
- Playbook (Logic Apps) で自動対応を実装する（例：不審なユーザーのアカウントを自動無効化）

### 監視すべき重要なログ
| ログ | 格納先 | 監視ポイント |
|---|---|---|
| Entra ID サインインログ | Log Analytics | 失敗したサインイン、不審な場所からのアクセス |
| Azure Activity Log | Log Analytics | リソースの作成・削除、RBAC の変更 |
| Key Vault 診断ログ | Log Analytics | 未認可のアクセス試行 |
| NSG フローログ | Storage Account + Traffic Analytics | 不審な通信パターン |
| AKS 診断ログ | Log Analytics | Pod の異常起動、特権コンテナ |

---

## コンプライアンスとガバナンス

### Azure Policy
- **Deny**: 非準拠リソースのデプロイを拒否
- **Audit**: 非準拠リソースを記録（デプロイは許可）
- **DeployIfNotExists**: 設定が不足している場合に自動でデプロイ
- **Modify**: 既存リソースのプロパティを修正

### 推奨ポリシーイニシアチブ
- **Azure Security Benchmark**: Azure のセキュリティベースライン
- **CIS Microsoft Azure Foundations Benchmark**: CIS ベースのセキュリティ標準
- **ISO 27001**: 情報セキュリティ管理
- **NIST SP 800-53**: 米国政府セキュリティ標準（グローバル展開時）
