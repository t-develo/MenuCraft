# Cycle 3: 認証（Register, Login, JWT, Refresh）

## 目的
ユーザー認証基盤を構築し、JWT によるステートレス認証を実現する。

## 成果物
- [x] `IJwtTokenService` / `JwtTokenService`
  - アクセストークン生成（HMAC-SHA256）
  - リフレッシュトークン生成（暗号論的乱数）
  - リフレッシュトークン形式検証
- [x] Auth DTO
  - `RegisterRequest`（Email, Password）
  - `LoginRequest`（Email, Password）
  - `RefreshRequest`（RefreshToken）
  - `AuthResponse`（AccessToken, RefreshToken, ExpiresAt）
- [x] `ApiResponse<T>` / `ApiResponse` エンベロープ DTO
- [x] `AuthFunction`
  - `POST /api/auth/register`（ユーザー登録 + JWT 発行）
  - `POST /api/auth/login`（パスワード検証 + JWT 発行）
  - `POST /api/auth/refresh`（リフレッシュトークン検証）
- [x] `JwtAuthenticationMiddleware`
  - 匿名ルートのスキップ（Health, AuthRegister, AuthLogin, AuthRefresh）
  - Bearer トークン抽出・検証
  - ClaimsPrincipal を FunctionContext.Items に設定
  - 認証失敗時の 401 レスポンス
- [x] `HttpRequestDataExtensions`
  - `GetUser()` — ClaimsPrincipal 取得
  - `GetUserId()` — ユーザー ID 取得
  - `GetFamilyGroupId()` — グループ ID 取得（nullable）
  - `RequireFamilyGroupId()` — グループ ID 取得（必須）
- [x] `Program.cs` にミドルウェア・JWT サービス登録
- [x] 単体テスト（`JwtTokenServiceTests`）
  - トークン生成検証
  - familyGroupId ありなし
  - リフレッシュトークン一意性
  - 無効トークン検証

## 依存関係
- Cycle 2（User エンティティ、AppDbContext）

## セキュリティ考慮事項
- パスワード: PBKDF2（ASP.NET Identity デフォルト）
- JWT シークレット: 環境変数から取得（ハードコーディング禁止）
- リフレッシュトークン: 64バイトの暗号論的乱数
- ClockSkew: 1 分（時刻ずれ許容）
- 認証エラーメッセージ: 「メールアドレスまたはパスワードが正しくありません」（情報漏洩防止）

## MVP 制限事項
- リフレッシュトークンの DB 永続化は未実装（Phase 2 で対応予定）
- レート制限は未実装（SWA の組み込みレート制限に依存）
