# MenuCraft — 技術ドキュメント

## 1. 開発環境セットアップ

### 前提条件
- .NET 10 SDK
- Azure Functions Core Tools v4
- Node.js (テスト実行用、任意)
- Git
- Visual Studio Code 推奨

### ローカル実行手順

```bash
# 1. リポジトリクローン
git clone https://github.com/t-develo/MenuCraft.git
cd MenuCraft

# 2. バックエンド依存関係復元
dotnet restore src/api/

# 3. local.settings.json を編集（DB 接続文字列、JWT シークレット）
cp src/api/local.settings.json.example src/api/local.settings.json
# ※ local.settings.json は .gitignore 対象

# 4. バックエンド実行
cd src/api && func start

# 5. フロントエンド（別ターミナル）
# Live Server 等で src/client/ を配信
```

### テスト実行

```bash
# バックエンドテスト
dotnet test tests/api.Tests/

# カバレッジ付きテスト
dotnet test tests/api.Tests/ --collect:"XPlat Code Coverage"
```

---

## 2. プロジェクト構成

```
menucraft/
├── .claude/              # Claude Code 設定（ルール、エージェント、スキル）
├── .github/workflows/    # CI/CD パイプライン
│   └── azure-swa.yml     # SWA デプロイワークフロー
├── docs/                 # 計画・設計ドキュメント
│   ├── PRD.md
│   ├── ARCHITECTURE.md
│   ├── SYSTEM_DESIGN.md
│   ├── TECHNICAL_NOTES.md
│   └── cycles/           # 各サイクルのタスクリスト
├── infra/                # Bicep テンプレート
│   └── main.bicep
├── src/
│   ├── api/              # Azure Functions (.NET 10)
│   │   ├── Data/         # EF Core DbContext
│   │   ├── Dtos/         # リクエスト/レスポンス DTO
│   │   ├── Extensions/   # ヘルパー拡張メソッド
│   │   ├── Functions/    # HTTP トリガー関数
│   │   ├── Middleware/   # JWT 認証ミドルウェア
│   │   ├── Models/       # エンティティモデル
│   │   ├── Repositories/ # データアクセス層
│   │   └── Services/     # ビジネスロジック層
│   └── client/           # Vanilla JS SPA
│       ├── css/
│       └── js/
│           ├── api/       # API クライアント
│           ├── components/# 再利用 UI コンポーネント
│           └── pages/     # ページ単位のロジック
├── tests/
│   └── api.Tests/        # xUnit テストプロジェクト
├── CLAUDE.md             # プロジェクトルール
├── staticwebapp.config.json  # SWA ルーティング設定
└── .gitignore
```

---

## 3. DI (依存性注入) 構成

`Program.cs` で登録されるサービス:

| サービス | ライフタイム | 説明 |
|----------|-------------|------|
| AppDbContext | Scoped | EF Core コンテキスト |
| UserManager\<User\> | Scoped | Identity ユーザー管理 |
| IJwtTokenService → JwtTokenService | Singleton | JWT 生成・検証 |
| IGroupRepository → GroupRepository | Scoped | グループデータアクセス |
| IGroupService → GroupService | Scoped | グループビジネスロジック |
| IRecipeRepository → RecipeRepository | Scoped | レシピデータアクセス |
| IRecipeService → RecipeService | Scoped | レシピビジネスロジック |
| IMealPlanRepository → MealPlanRepository | Scoped | 献立データアクセス |
| IMealPlanService → MealPlanService | Scoped | 献立ビジネスロジック |
| IShoppingService → ShoppingService | Scoped | 買い物リストロジック |
| IOgpService → OgpService | Scoped | OGP メタデータ取得 |

---

## 4. JWT トークン仕様

| 項目 | 値 |
|------|-----|
| アルゴリズム | HMAC-SHA256 |
| Issuer | 環境変数 `Jwt:Issuer` |
| Audience | 環境変数 `Jwt:Audience` |
| 有効期限 | 60 分（環境変数で変更可能）|
| ClockSkew | 1 分 |

### Claims

| Claim | 説明 |
|-------|------|
| `sub` (NameIdentifier) | ユーザー ID (GUID) |
| `email` | メールアドレス |
| `jti` | トークン ID (GUID) |
| `familyGroupId` | 所属グループ ID (int、未所属の場合なし) |

---

## 5. EF Core マイグレーション

```bash
# マイグレーション追加
dotnet ef migrations add <MigrationName> --project src/api/ --startup-project src/api/

# データベース更新
dotnet ef database update --project src/api/ --startup-project src/api/

# マイグレーション一覧
dotnet ef migrations list --project src/api/
```

### 注意事項
- マイグレーションは `src/api/Migrations/` に生成される
- 初回デプロイ時は `EnsureCreated()` またはマイグレーション適用が必要
- Free Tier では接続数制限があるため、接続プーリングを適切に設定

---

## 6. 環境変数 / 設定一覧

### local.settings.json (ローカル開発用、Git 管理外)

| キー | 説明 | 例 |
|------|------|-----|
| ConnectionStrings__Default | SQL Server 接続文字列 | `Server=...;Database=menucraft-db;...` |
| Jwt__Secret | JWT 署名キー（32文字以上） | ランダム文字列 |
| Jwt__Issuer | JWT 発行者 | `https://localhost` |
| Jwt__Audience | JWT 対象者 | `menucraft-api` |
| Jwt__AccessTokenExpirationMinutes | アクセストークン有効期限（分） | `60` |
| Jwt__RefreshTokenExpirationDays | リフレッシュトークン有効期限（日） | `7` |

### Azure 環境（SWA アプリケーション設定 / Key Vault）

- 本番シークレットは Azure Key Vault に保管
- SWA アプリケーション設定から Key Vault 参照で取得
- Managed Identity を使用（資格情報不要）

---

## 7. テスト戦略

### テスト種別

| 種別 | フレームワーク | 対象 |
|------|--------------|------|
| 単体テスト | xUnit + Moq + FluentAssertions | Service, Repository |
| 統合テスト | xUnit + EF Core InMemory | DbContext, データモデル |
| E2E テスト | Playwright (予定) | クリティカルユーザーフロー |

### テストファイル配置

```
tests/api.Tests/
├── Data/
│   └── AppDbContextTests.cs       # 統合テスト
├── Functions/
│   └── HealthFunctionTests.cs     # Function テスト
├── Services/
│   ├── JwtTokenServiceTests.cs    # JWT テスト
│   ├── GroupServiceTests.cs       # グループテスト
│   ├── RecipeServiceTests.cs      # レシピテスト
│   ├── MealPlanServiceTests.cs    # 献立テスト
│   └── ShoppingServiceTests.cs   # 買い物リストテスト
└── Repositories/
    └── (統合テストで代用)
```

### カバレッジ目標: 80% 以上

---

## 8. フロントエンド規約

### API 呼び出しパターン

```javascript
// すべての API 呼び出しは apiFetch() を経由
// - JWT 自動付与
// - 401 時に自動ログインリダイレクト
const response = await apiFetch('/api/recipes');
const { success, data, error } = await response.json();
```

### DOM 操作セキュリティ

```javascript
// テキスト表示: 常に textContent を使用
element.textContent = userInput;

// HTML 表示が必要な場合: DOMPurify 必須
element.innerHTML = DOMPurify.sanitize(htmlContent);

// 動的 HTML: createElement + appendChild
const el = document.createElement('div');
el.textContent = data;
container.appendChild(el);
```

### 禁止事項
- `innerHTML` にユーザー入力を直接代入しない
- `eval()` / `new Function()` 使用禁止
- `console.log` を本番コードに残さない
- `var` 使用禁止（`const` / `let` のみ）
