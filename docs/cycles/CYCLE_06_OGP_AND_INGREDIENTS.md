# Cycle 6: OGP メタデータ取得 & 材料テキスト解析

## 目的
URL からレシピのメタデータ（タイトル、画像）を自動取得する機能と、テキストから材料を自動パースする機能を実装する。

## タスクリスト
- [ ] `IOgpService` / `OgpService`
  - URL バリデーション（HTTP/HTTPS のみ）
  - プライベート IP / ローカルホストへのアクセス禁止（SSRF 対策）
  - HTML 取得（HttpClient、タイムアウト 10 秒、サイズ上限 1MB）
  - `og:title`, `og:image`, `og:description` メタタグ抽出
  - OGP がない場合は `<title>` タグにフォールバック
- [ ] `IIngredientParserService` / `IngredientParserService`
  - テキスト行を材料に分割
  - 正規表現で「名前 数量 単位」パターンを抽出
  - 日本語の一般的な単位（g, ml, 個, 本, 枚, 大さじ, 小さじ, カップ 等）対応
- [ ] OGP DTO
  - `FetchOgpRequest`（Url）
  - `OgpResponse`（Title, ImageUrl, Description）
- [ ] 材料パース DTO
  - `ParseIngredientsRequest`（Text）
  - `ParseIngredientsResponse`（Ingredients: IngredientResponse[]）
- [ ] `RecipeFunction` に追加エンドポイント
  - `POST /api/recipes/fetch-ogp`
  - `POST /api/recipes/parse-ingredients`
- [ ] フロントエンド
  - レシピフォームに URL 入力時の「自動取得」ボタン追加
  - 材料テキストエリアに「自動解析」ボタン追加
- [ ] 単体テスト
  - OGP パース（正常系、OGP なし、タイムアウト）
  - 材料パース（正常系、各種フォーマット）
  - SSRF 対策テスト

## 依存関係
- Cycle 5（RecipeFunction、レシピフォーム）

## セキュリティ考慮事項
- OGP フェッチは**サーバーサイドのみ**で実行（クライアントからの直接フェッチ禁止）
- プライベート IP レンジへのリクエストをブロック（10.x, 172.16-31.x, 192.168.x, 127.x）
- リダイレクトの追跡は最大 5 回まで
- レスポンスサイズ上限 1MB
