# Cycle 4: 家族グループ管理

## 目的
家族グループの作成・参加・メンバー一覧機能を実装し、データ分離の基盤を確立する。

## 成果物
- [x] `IGroupRepository` / `GroupRepository`
  - `FindByIdAsync` — ID でグループ検索
  - `FindByInviteCodeAsync` — 招待コードでグループ検索
  - `CreateAsync` — グループ作成
  - `GetMembersAsync` — メンバー一覧取得
- [x] `IGroupService` / `GroupService`
  - `CreateGroupAsync` — グループ作成 + ユーザー割り当て + 招待コード生成
  - `JoinGroupAsync` — 招待コードでグループ参加
  - `GetMembersAsync` — メンバー一覧取得
- [x] Group DTO
  - `CreateGroupRequest`（Name）
  - `JoinGroupRequest`（InviteCode）
  - `GroupResponse`（Id, Name, InviteCode）
  - `MemberResponse`（Id, Email）
- [x] `GroupFunction`
  - `POST /api/groups` — グループ作成
  - `POST /api/groups/join` — グループ参加
  - `GET /api/groups/members` — メンバー一覧
- [x] フロントエンド
  - `js/pages/groupSetup.js` — グループ作成/参加フォーム
- [x] `Program.cs` に Repository / Service 登録
- [x] 単体テスト（`GroupServiceTests`）
  - グループ作成成功
  - 既にグループ所属時のエラー
  - 招待コードで参加成功
  - 無効な招待コードのエラー
  - メンバー一覧取得

## 依存関係
- Cycle 3（認証、JWT ミドルウェア、Extensions）

## 設計判断
- 招待コード: 8文字の英数字（紛らわしい文字 0, O, I, 1 を除外）
- 1 ユーザー = 1 グループの制約（MVP）
- 招待コードはグループ作成時に自動生成、ユニーク制約あり
