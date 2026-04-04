# 管理システム・マイページ・リフレッシュトークン実装計画

## 概要

MenuCraft に以下の機能を追加する実装計画。

1. **リフレッシュトークン** — DB保存、トークンローテーション、マルチデバイス対応
2. **ユーザーロール（Admin/User）** — 権限ベースのアクセス制御
3. **管理画面 API** — ユーザー一覧・ロール変更、グループ管理・招待コード再生成
4. **マイページ API** — グループ情報確認、招待コード表示、パスワード変更、グループ脱退
5. **フロントエンド** — 管理画面UI・マイページUI・自動トークンリフレッシュ・ルーティング拡張

### 決定事項

- 最初の登録ユーザーを自動的にAdminにする
- マイページにはパスワード変更を含める
- 招待コード再生成機能を含める

---

## Phase 1: リフレッシュトークン基盤

### 目的

現在リフレッシュトークンは未実装（401を返す状態）。マルチデバイスでのログイン維持と、後続フェーズでのロール/グループ変更後のJWT再発行に必要な基盤を構築する。

### やる事

1. **RefreshToken モデル作成** — `src/api/Models/RefreshToken.cs`
   - フィールド: Id (Guid PK), UserId (Guid FK→User), Token (string, unique, max 128), ExpiresAt, CreatedAt, IsRevoked (bool), DeviceInfo (string?)
   - 有効期限: 30日（`Jwt:RefreshTokenExpirationDays` で設定可能）

2. **AppDbContext 変更** — `src/api/Data/AppDbContext.cs`
   - `DbSet<RefreshToken>` 追加
   - OnModelCreating にエンティティ設定（FK, unique index on Token, index on UserId）

3. **RefreshTokenRepository 作成** — `src/api/Repositories/IRefreshTokenRepository.cs`, `RefreshTokenRepository.cs`
   - FindByTokenAsync, CreateAsync, RevokeAsync, RevokeAllForUserAsync, DeleteExpiredAsync

4. **AuthFunction 変更** — `src/api/Functions/AuthFunction.cs`
   - Register/Login: リフレッシュトークンをDB保存
   - Refresh: DBからトークン検索 → 有効なら旧トークン失効 → 新トークンペア発行（トークンローテーション）

5. **DI登録** — `src/api/Program.cs`
   - `services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>()` 追加

### ゴール

- Register/Login で返却されるリフレッシュトークンがDBに保存される
- `POST /api/auth/refresh` で有効なリフレッシュトークンを送ると新しいアクセストークン+リフレッシュトークンが返却される
- 使用済みリフレッシュトークンは失効し再利用できない（トークンローテーション）

### 確認方法

- `dotnet build src/api/` がエラーなく通ること
- 既存テスト（`dotnet test`）がパスすること
- AuthFunction の Refresh メソッドが 401 固定ではなく、DB検索ベースの処理に変わっていること

### 注意点

- `IJwtTokenService.ValidateRefreshToken` は Phase 2 で削除予定。Phase 1 では AuthFunction 内でDB検索に切り替えるが、インターフェースの変更は Phase 2 にまとめる
- リフレッシュトークンの有効期限設定は `local.settings.json` に追加が必要
- マイグレーションは DB接続がないローカル環境では実行できないため、コード変更のみ実施

---

## Phase 2: ユーザーロール（Admin/User）

### 目的

全ユーザーが同等の権限を持つ現状から、Admin と User の2ロールを導入し、管理画面APIの認可基盤を構築する。

### やる事

1. **Identity にロール機能を追加** — `src/api/Program.cs`
   - `.AddRoles<IdentityRole<Guid>>()` を Identity 設定に追加

2. **RoleSeeder 作成** — `src/api/Data/RoleSeeder.cs`
   - 起動時に "Admin" と "User" ロールを RoleManager で作成（冪等）
   - Program.cs の `host.Build()` 後、`host.Run()` 前に呼び出し

3. **JwtTokenService 変更** — `src/api/Services/IJwtTokenService.cs`, `JwtTokenService.cs`
   - `GenerateAccessToken(User user)` → `GenerateAccessToken(User user, string role)` に変更
   - クレームに `new Claim("role", role)` を追加（短い名前を使用。`ClaimTypes.Role` はURIが長くフロントエンドで扱いにくいため避ける）
   - `ValidateRefreshToken` メソッドを削除（Phase 1 でDB検索に移行済み）

4. **AuthFunction 変更** — `src/api/Functions/AuthFunction.cs`
   - Register: ユーザー作成後、`_userManager.Users.CountAsync() == 1` なら "Admin"、それ以外は "User" ロールを付与
   - Login: 既存ユーザーにロールがない場合（既存データ対応）、"User" を自動付与
   - アクセストークン生成時にロールを渡す

5. **GroupFunction 変更** — `src/api/Functions/GroupFunction.cs`
   - `GenerateAccessToken` 呼び出し箇所をロール付きに更新
   - UserManager をコンストラクタインジェクションに追加

6. **認可ヘルパー追加** — `src/api/Extensions/HttpRequestDataExtensions.cs`
   - `GetUserRole(this FunctionContext)`: JWTの "role" クレームを取得
   - `RequireAdmin(this FunctionContext)`: Admin以外なら例外スロー

### ゴール

- 新規登録の最初のユーザーに Admin ロールが付与される
- 2人目以降のユーザーに User ロールが付与される
- JWT のペイロードに `"role": "Admin"` または `"role": "User"` が含まれる
- `RequireAdmin()` 拡張メソッドが利用可能になる

### 確認方法

- `dotnet build src/api/` がエラーなく通ること
- 既存テスト（`dotnet test`）がパスすること
- JwtTokenService の GenerateAccessToken シグネチャが `(User user, string role)` に変わっていること
- AuthFunction, GroupFunction の全ての GenerateAccessToken 呼び出しがロール付きになっていること

### 注意点

- `GenerateAccessToken` のシグネチャ変更は破壊的変更。AuthFunction, GroupFunction の全呼び出し箇所を漏れなく更新すること
- 既存ユーザーのロール未設定問題: Login時のフォールバックで "User" を付与する処理が必要
- 初回Admin判定の競合: 同時登録で2人ともAdminになるリスクがあるが、個人利用のため許容

---

## Phase 3: 管理画面 API

### 目的

Admin ユーザーがアプリ全体のユーザーとグループを管理できるAPIを提供する。

### やる事

1. **Admin用 DTO 作成** — `src/api/Dtos/Admin/`
   - `UserListResponse.cs`: record(Guid Id, string Email, string Role, int? FamilyGroupId, string? GroupName, DateTime CreatedAt)
   - `ChangeRoleRequest.cs`: record(string Role) — "Admin" or "User" のみ
   - `GroupListResponse.cs`: record(int Id, string Name, string InviteCode, int MemberCount, DateTime CreatedAt)
   - `UpdateGroupRequest.cs`: record(string Name)

2. **AdminService 作成** — `src/api/Services/IAdminService.cs`, `AdminService.cs`
   - GetAllUsersAsync, ChangeUserRoleAsync, GetAllGroupsAsync, UpdateGroupNameAsync, RemoveMemberFromGroupAsync, DeleteGroupAsync, RegenerateInviteCodeAsync
   - ビジネスルール: 最後のAdminの降格は拒否

3. **FamilyGroup モデル変更** — `src/api/Models/FamilyGroup.cs`
   - `Name` と `InviteCode` を `init` → `set` に変更（更新可能にする）

4. **GroupRepository 拡張** — `src/api/Repositories/IGroupRepository.cs`, `GroupRepository.cs`
   - GetAllAsync, UpdateAsync, DeleteAsync を追加

5. **招待コード生成の共有化**
   - `GroupService.GenerateInviteCode` を static ユーティリティに抽出し、AdminService からも利用可能にする

6. **AdminFunction 作成** — `src/api/Functions/AdminFunction.cs`
   - GET `admin/users` — 全ユーザー一覧
   - PUT `admin/users/{userId}/role` — ロール変更
   - GET `admin/groups` — 全グループ一覧
   - PUT `admin/groups/{groupId}` — グループ名編集
   - DELETE `admin/groups/{groupId}` — グループ削除
   - DELETE `admin/groups/{groupId}/members/{userId}` — メンバー除外
   - POST `admin/groups/{groupId}/invite-code` — 招待コード再生成
   - 全エンドポイントで `context.RequireAdmin()` を呼び出し

7. **DI登録** — `src/api/Program.cs`
   - `services.AddScoped<IAdminService, AdminService>()` 追加

### ゴール

- Admin ユーザーが全ユーザーの一覧取得・ロール変更ができる
- Admin ユーザーが全グループの一覧取得・名前編集・削除・メンバー除外・招待コード再生成ができる
- User ロールのユーザーが admin API にアクセスすると 403 が返る
- 最後の Admin を降格しようとするとエラーが返る

### 確認方法

- `dotnet build src/api/` がエラーなく通ること
- 既存テスト（`dotnet test`）がパスすること
- AdminFunction の全エンドポイントに `RequireAdmin()` が含まれていること
- ChangeUserRoleAsync に最後のAdmin降格防止ロジックがあること

### 注意点

- `FamilyGroup.Name`/`InviteCode` の `init` → `set` 変更はDBスキーマに影響しないが、既存コードのオブジェクト初期化パターンが壊れないか確認
- グループ削除時: 既存の `OnDelete(SetNull)` でメンバーの FamilyGroupId が null になり、`OnDelete(Cascade)` でレシピ・献立・買い物リストが削除される
- 招待コード生成ロジックの共有化時、GroupService の既存動作を壊さないこと

---

## Phase 4: マイページ API

### 目的

全ユーザーが自分のアカウント情報を確認し、パスワード変更やグループ脱退を行えるAPIを提供する。

### やる事

1. **Profile用 DTO 作成** — `src/api/Dtos/Profile/`
   - `ProfileResponse.cs`: record(Guid Id, string Email, string Role, int? FamilyGroupId, string? GroupName, string? InviteCode, DateTime CreatedAt)
   - `ChangePasswordRequest.cs`: record(string CurrentPassword, string NewPassword)

2. **ProfileFunction 作成** — `src/api/Functions/ProfileFunction.cs`
   - GET `profile` — 自分のプロフィール情報取得
     - userId → User取得 → ロール取得 → FamilyGroup情報取得 → ProfileResponse返却
   - PUT `profile/password` — パスワード変更
     - `UserManager.ChangePasswordAsync` → 全リフレッシュトークン失効 → 新トークンペア発行
   - POST `profile/leave-group` — グループ脱退
     - `FamilyGroupId = null` に設定 → 新アクセストークン（familyGroupIdクレームなし）を発行

### ゴール

- 認証済みユーザーが自分のプロフィール情報（メール、ロール、グループ名、招待コード）を取得できる
- パスワード変更が成功すると他デバイスのリフレッシュトークンが全て失効する
- グループ脱退後、familyGroupId を含まない新しいアクセストークンが返却される

### 確認方法

- `dotnet build src/api/` がエラーなく通ること
- 既存テスト（`dotnet test`）がパスすること
- ProfileFunction の各エンドポイントが認証必須であること（JwtAuthenticationMiddleware のanonymousリストに含まれないこと）
- パスワード変更後に RevokeAllForUserAsync が呼ばれていること

### 注意点

- パスワード変更後のトークン再発行では、新しいリフレッシュトークンもDB保存が必要
- グループ脱退はユーザーの FamilyGroupId を null にするだけで、グループ自体は削除しない
- NewPassword のバリデーション（8文字以上）は既存の Identity 設定に従う

---

## Phase 5: フロントエンド

### 目的

Phase 1〜4 で構築した API をフロントエンドから利用できるようにし、管理画面・マイページのUIとトークン自動更新を実装する。

### やる事

1. **apiFetch.js 変更** — リフレッシュトークン自動更新
   - 401受信時、ログインリダイレクト前に `POST /api/auth/refresh` を試行
   - 成功 → 新トークン保存 → 元リクエストをリトライ
   - 失敗 → トークン削除 → ログインへリダイレクト
   - 無限ループ防止用の `_isRefreshing` フラグ

2. **AdminApi モジュール作成** — `src/client/js/api/admin.js`
   - getUsers, changeRole, getGroups, updateGroup, deleteGroup, removeMember, regenerateInviteCode

3. **ProfileApi モジュール作成** — `src/client/js/api/profile.js`
   - getProfile, changePassword, leaveGroup

4. **管理画面ページ作成** — `src/client/js/pages/admin.js`
   - `renderAdminPage(container)` 関数
   - ユーザータブ: テーブル表示（メール、ロール、グループ名、登録日）、ロール変更ボタン
   - グループタブ: テーブル表示（名前、招待コード、メンバー数、作成日）、名前編集、削除、メンバー管理、招待コード再生成

5. **マイページ作成** — `src/client/js/pages/myPage.js`
   - `renderMyPage(container)` 関数
   - アカウント情報セクション: メール、ロール表示
   - グループ情報セクション: グループ名、招待コード（コピーボタン付き）、脱退ボタン
   - パスワード変更セクション: 現在のパスワード、新パスワード、確認入力のフォーム

6. **app.js 変更** — ルーティング拡張
   - JWTペイロードから `role` クレームを取得
   - ナビに「マイページ」を全ユーザー向けに追加（route: `mypage`）
   - ナビに「管理」をAdmin限定で追加（route: `admin`）
   - `navigateTo()` に mypage, admin のルーティングを追加

7. **index.html 変更** — 新スクリプトタグ追加

8. **style.css 変更** — 管理画面・マイページ用スタイル追加
   - `.admin-page`, `.admin-tabs`, `.admin-table`
   - `.mypage`, `.mypage-section`
   - `.invite-code-display`

### ゴール

- 401時にリフレッシュトークンで自動更新され、ユーザーが再ログイン不要になる
- Admin ユーザーのナビに「管理」リンクが表示され、管理画面でユーザー/グループを操作できる
- 全ユーザーのナビに「マイページ」リンクが表示され、プロフィール確認・パスワード変更・グループ脱退ができる
- User ロールのユーザーには「管理」リンクが表示されない

### 確認方法

- ブラウザで各ページが正常に表示されること（レイアウト崩れなし）
- `console.log` が残っていないこと
- `innerHTML` の直接代入がないこと（XSS防止、textContent または createElement を使用）
- Admin以外で `#admin` にアクセスした場合、APIが403を返しエラーメッセージが表示されること

### 注意点

- apiFetch.js のリフレッシュ処理で無限ループ防止が必須（リフレッシュAPI自体が401を返す場合）
- JWTの `role` クレームはフロントエンドでの表示制御のみに使用し、実際の認可はバックエンド側で行う
- 管理画面の操作（グループ削除等）には確認ダイアログを入れること
- 日本語UIテキストの統一（既存ページと同じトーン）

---

## ファイル変更一覧

### 新規ファイル（18件）

| ファイル | Phase |
|---------|-------|
| `src/api/Models/RefreshToken.cs` | 1 |
| `src/api/Repositories/IRefreshTokenRepository.cs` | 1 |
| `src/api/Repositories/RefreshTokenRepository.cs` | 1 |
| `src/api/Data/RoleSeeder.cs` | 2 |
| `src/api/Dtos/Admin/UserListResponse.cs` | 3 |
| `src/api/Dtos/Admin/ChangeRoleRequest.cs` | 3 |
| `src/api/Dtos/Admin/GroupListResponse.cs` | 3 |
| `src/api/Dtos/Admin/UpdateGroupRequest.cs` | 3 |
| `src/api/Services/IAdminService.cs` | 3 |
| `src/api/Services/AdminService.cs` | 3 |
| `src/api/Functions/AdminFunction.cs` | 3 |
| `src/api/Dtos/Profile/ProfileResponse.cs` | 4 |
| `src/api/Dtos/Profile/ChangePasswordRequest.cs` | 4 |
| `src/api/Functions/ProfileFunction.cs` | 4 |
| `src/client/js/api/admin.js` | 5 |
| `src/client/js/api/profile.js` | 5 |
| `src/client/js/pages/admin.js` | 5 |
| `src/client/js/pages/myPage.js` | 5 |

### 変更ファイル（14件）

| ファイル | Phase | 変更内容 |
|---------|-------|---------|
| `src/api/Data/AppDbContext.cs` | 1 | RefreshToken DbSet + エンティティ設定 |
| `src/api/Program.cs` | 1,2,3 | DI登録、`.AddRoles<>()`、RoleSeeder呼び出し |
| `src/api/Services/IJwtTokenService.cs` | 2 | GenerateAccessToken にroleパラメータ追加 |
| `src/api/Services/JwtTokenService.cs` | 2 | roleクレーム追加、ValidateRefreshToken 削除 |
| `src/api/Functions/AuthFunction.cs` | 1,2 | リフレッシュトークンDB保存、ロール割り当て |
| `src/api/Functions/GroupFunction.cs` | 2 | GenerateAccessTokenにロール渡し |
| `src/api/Extensions/HttpRequestDataExtensions.cs` | 2 | GetUserRole, RequireAdmin 追加 |
| `src/api/Models/FamilyGroup.cs` | 3 | Name/InviteCode を init→set |
| `src/api/Repositories/IGroupRepository.cs` | 3 | GetAll/Update/Delete 追加 |
| `src/api/Repositories/GroupRepository.cs` | 3 | 同上の実装 |
| `src/client/js/app.js` | 5 | ロール取得、ナビ追加、新ルート |
| `src/client/js/api/apiFetch.js` | 5 | 401時の自動リフレッシュ |
| `src/client/index.html` | 5 | 新スクリプトタグ |
| `src/client/css/style.css` | 5 | 管理画面・マイページ用スタイル |

---

## 横断的な注意点

1. **既存ユーザーのロール未設定**: デプロイ後、既存ユーザーにはロールがない。Login時にロールが空なら "User" を自動付与するフォールバックを実装
2. **ClaimTypes.Role のURI問題**: `ClaimTypes.Role` は長いURIになる。代わりに `"role"` という短い文字列をクレーム名に使用
3. **招待コード生成の共有化**: `GroupService.GenerateInviteCode` は private メソッド。AdminService からも使えるように static ユーティリティに抽出
4. **トークンローテーションの競合**: 同時に2つのデバイスがリフレッシュを試みた場合、一方が失敗する可能性がある。個人利用（最大5接続）のため許容
