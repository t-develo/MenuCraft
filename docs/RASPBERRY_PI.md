# Raspberry Pi でのローカル実行

MenuCraft を家庭内 LAN の Raspberry Pi 上で常時稼働させるための手順書。

Azure 版との違いは次のとおり。

| | Azure | Raspberry Pi |
|---|---|---|
| フロントエンド | Storage Account 静的 Web サイト | nginx (`/var/www/menucraft`) |
| API | Azure Functions (Consumption) | Azure Functions Core Tools + systemd |
| データベース | Azure SQL Database | **SQLite** (`/var/lib/menucraft/menucraft.db`) |
| CORS | `AllowedOrigins` で許可 | nginx が `/api/` を中継するため**同一オリジン**、設定不要 |
| HTTPS | 既定で有効 | **なし（LAN 内 HTTP のみ）** |

---

## 前提条件

### 必須: 64bit OS

**.NET は 32bit ARM (armv7l) をサポートしていません。** 64bit 版の OS が必要です。

```bash
uname -m
# aarch64 と表示されればOK
# armv7l と表示された場合は 64bit 版の Raspberry Pi OS を入れ直してください
```

### 推奨環境

- Raspberry Pi 4 / 5（メモリ 2GB 以上）
- Raspberry Pi OS (64-bit) または Ubuntu Server for Raspberry Pi
- 有線 LAN 接続、または安定した Wi-Fi
- ストレージ 8GB 以上の空き（.NET SDK と Core Tools で約 1.5GB 使用）

> **SD カードの寿命について**: SQLite の書き込みと journald のログで SD カードは消耗します。
> 長期運用するなら USB SSD からの起動を推奨します。最低限の対策として、
> `setup.sh` は journald のログ保持量を 200MB に制限し、DB の日次バックアップを設定します。

### 事前設定（任意だが推奨）

```bash
# ホスト名を menucraft にすると http://menucraft.local でアクセスできる（avahi/mDNS）
sudo hostnamectl set-hostname menucraft

# タイムゾーン（ログの可読性のため。アプリのロジックには影響しません）
sudo timedatectl set-timezone Asia/Tokyo
```

---

## セットアップ

```bash
git clone https://github.com/t-develo/MenuCraft.git
cd MenuCraft
sudo ./deploy/raspi/setup.sh
```

所要時間は 10〜20 分程度（.NET SDK のダウンロードとビルドが大半）。

`setup.sh` が行うこと:

1. アーキテクチャ（aarch64）と systemd/apt の存在を確認
2. `nginx` `sqlite3` `curl` `unzip` `rsync` `openssl` を APT でインストール
3. 実行ユーザー `menucraft`（ログインシェルなし）と各ディレクトリを作成
4. .NET を `/usr/share/dotnet` に用意（SDK と、`TargetFramework` に対応するランタイム）
5. Azure Functions Core Tools (linux-arm64) を `/opt/menucraft/core-tools` にインストール
6. `/etc/menucraft/menucraft.env` を生成し、JWT シークレットをランダム生成
7. API をビルドして `/opt/menucraft/api` に配置
8. フロントエンドを `/var/www/menucraft` に配置し、`API_BASE_URL` を空文字に置換
9. systemd ユニットと nginx 設定を配置して有効化
10. ヘルスチェックで疎通を確認

**冪等です。** 途中で失敗した場合も、原因を解消してから再実行して構いません。

### .NET のバージョンについて

API は `src/api/MenuCraft.Api.csproj` の `TargetFramework`（現在 `net10.0`）でビルドされ、
**実行にも同じメジャーバージョンのランタイムが必要**です。.NET は既定でメジャーバージョンを
跨いでロールフォワードしないため、別のメジャーバージョン（例: .NET 8）だけが入っている
環境では、ビルドは通っても worker が起動できません。

`setup.sh` はこれを検出し、必要なランタイムを side-by-side で追加インストールします。
既存の .NET は削除・変更しません。
既存の `menucraft.env` とデータベースは上書きされません。

### 初回ログイン

ブラウザで `http://menucraft.local/`（または `http://<ラズパイのIP>/`）を開き、**新規登録**します。

**最初に登録したユーザーが自動的に管理者 (Admin) になります**
（`src/api/Functions/AuthFunction.cs` の登録処理）。2 人目以降は一般ユーザーです。

---

## ディレクトリ構成

| パス | 内容 |
|---|---|
| `/opt/menucraft/api/` | API の発行成果物 |
| `/opt/menucraft/core-tools/` | Azure Functions Core Tools |
| `/opt/menucraft/backup.sh` | バックアップスクリプト |
| `/var/www/menucraft/` | フロントエンドの静的ファイル |
| `/var/lib/menucraft/menucraft.db` | **SQLite データベース（唯一の永続データ）** |
| `/var/lib/menucraft/home/` | Core Tools 用の `HOME`（`.azurefunctions` などの作業ファイル）|
| `/etc/menucraft/menucraft.env` | 環境変数・シークレット (640, root:menucraft) |
| `/var/backups/menucraft/` | DB バックアップ (gzip, 14世代) |

---

## 環境変数

`/etc/menucraft/menucraft.env` で設定します。編集後は `sudo systemctl restart menucraft-api` が必要です。

階層キー（`Jwt:Secret` など）は二重アンダースコア（`Jwt__Secret`）で表現します。

| 変数 | 説明 |
|---|---|
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` 固定 |
| `AzureWebJobsStorage` | HTTP トリガーのみのため空でよい |
| `DOTNET_ROOT` | `/usr/share/dotnet` |
| `Database__Provider` | `Sqlite`（Azure は `SqlServer`。未設定時の既定は `SqlServer`） |
| `ConnectionStrings__Default` | `Data Source=/var/lib/menucraft/menucraft.db` |
| `Jwt__Secret` | `setup.sh` が生成。32 文字以上必須 |
| `Jwt__Issuer` / `Jwt__Audience` | JWT の発行者・対象 |
| `Jwt__AccessTokenExpirationMinutes` | アクセストークン有効期限（既定 60 分） |
| `AllowedOrigins` | nginx 経由なら空でよい |
| `Seed__AdminEmail` / `Seed__AdminPassword` | 任意。両方設定した場合のみ起動時に管理者を作成 |

### 管理者権限を復旧したい場合

管理者アカウントを失った場合は、`Seed__AdminEmail` と `Seed__AdminPassword` を設定して再起動します。

```bash
sudo nano /etc/menucraft/menucraft.env
# Seed__AdminEmail=rescue@menucraft.local
# Seed__AdminPassword=<十分に長いパスワード>
sudo systemctl restart menucraft-api
```

起動時に該当アカウントが作成され、Admin ロールが付与されます。
既に同じメールのユーザーがいる場合はパスワードを変更せず、Admin ロールの付与だけを行います。

**復旧後は両方の値を空に戻して再起動してください**（設定ファイルに平文パスワードを残さないため）。

---

## 運用コマンド

```bash
# 状態確認
sudo systemctl status menucraft-api

# ログをリアルタイム表示
sudo journalctl -u menucraft-api -f

# 直近 100 行のログ
sudo journalctl -u menucraft-api -n 100 --no-pager

# 再起動 / 停止 / 開始
sudo systemctl restart menucraft-api
sudo systemctl stop menucraft-api
sudo systemctl start menucraft-api

# 疎通確認
curl http://127.0.0.1/api/health      # → {"status":"ok"}
```

### 自動起動

`setup.sh` が `systemctl enable` 済みのため、**ラズパイの再起動後も自動的に起動します**。

```bash
# 有効になっているか確認
systemctl is-enabled menucraft-api nginx menucraft-backup.timer
# → enabled / enabled / enabled

# 実際に再起動して確認
sudo reboot
# 起動後（1〜2分後）
curl http://menucraft.local/api/health
```

プロセスが異常終了した場合も `Restart=always` により 10 秒後に自動復帰します。

---

## 更新

```bash
cd ~/MenuCraft
sudo ./deploy/raspi/deploy.sh --pull   # git pull してからデプロイ
sudo ./deploy/raspi/deploy.sh          # 手元の変更をそのままデプロイ
```

ビルドが成功してからサービスを停止するため、**ビルド失敗時に停止時間は発生しません**。

> **注意**: エンティティモデル（`src/api/Models/`）を変更した場合は「データベースのスキーマ変更」の項を参照してください。

---

## バックアップとリストア

日次バックアップが `menucraft-backup.timer` により 04:00 前後に実行されます（14 世代保持）。

```bash
# タイマーの状態
systemctl list-timers menucraft-backup.timer

# 手動でバックアップ
sudo systemctl start menucraft-backup.service

# バックアップ一覧
ls -lh /var/backups/menucraft/
```

単純なファイルコピーではなく `sqlite3 .backup` を使っています。
WAL モードでは直近の書き込みが `menucraft.db-wal` 側にあるため、`.db` だけをコピーすると
データを取りこぼすためです。作成後に `PRAGMA integrity_check` で検証しています。

### リストア

```bash
sudo systemctl stop menucraft-api

# 念のため現行 DB を退避
sudo mv /var/lib/menucraft/menucraft.db /var/lib/menucraft/menucraft.db.bak
sudo rm -f /var/lib/menucraft/menucraft.db-wal /var/lib/menucraft/menucraft.db-shm

# バックアップを展開して配置
sudo gunzip -c /var/backups/menucraft/menucraft-20260804-040000.db.gz \
  | sudo tee /var/lib/menucraft/menucraft.db > /dev/null
sudo chown menucraft:menucraft /var/lib/menucraft/menucraft.db

sudo systemctl start menucraft-api
curl http://127.0.0.1/api/health
```

### 母艦 PC へバックアップを退避

SD カードごと壊れると意味がないため、定期的に別のマシンへコピーすることを推奨します。

```bash
scp pi@menucraft.local:/var/backups/menucraft/*.db.gz ~/menucraft-backups/
```

---

## データベースのスキーマ変更

**これは既知の制限です。**

SQLite のスキーマは起動時に EF Core の `EnsureCreated()` で作成されます。
`EnsureCreated()` は「テーブルが無ければ作る」だけで、**既存テーブルの変更には追従しません**。

エンティティモデルを変更した場合は、次のいずれかが必要です。

**A. データを捨ててよい場合（開発中など）**

```bash
sudo systemctl stop menucraft-api
sudo rm -f /var/lib/menucraft/menucraft.db*
sudo systemctl start menucraft-api   # 新しいスキーマで再作成される
```

**B. データを保持する場合**

バックアップを取ったうえで `sqlite3` で手動 `ALTER TABLE` を実行します。

```bash
sudo systemctl start menucraft-backup.service   # 先にバックアップ
sudo -u menucraft sqlite3 /var/lib/menucraft/menucraft.db
sqlite> ALTER TABLE Recipes ADD COLUMN Servings INTEGER;
sqlite> .quit
sudo systemctl restart menucraft-api
```

将来的にモデル変更が増えるようなら、EF Core Migrations の導入を検討してください。

---

## セキュリティ

このセットアップは**家庭内 LAN からの HTTP アクセス**を前提にしています。

- 認証は JWT（アクセストークン 60 分、リフレッシュトークン 30 日）
- `noindex` / `robots.txt` と nginx の `X-Robots-Tag` で検索エンジンに露出しない
- Functions ホストの管理エンドポイント `/admin/*` は nginx で 404 にして遮断
- **通信は暗号化されていません。** ルーターのポート開放は絶対に行わないでください

### ufw で LAN 内に限定する（推奨）

```bash
sudo apt-get install -y ufw
sudo ufw allow from 192.168.0.0/16 to any port 80 proto tcp   # 自宅のサブネットに合わせる
sudo ufw allow from 192.168.0.0/16 to any port 22 proto tcp   # SSH を締め出さないこと
sudo ufw enable
sudo ufw status verbose
```

> サブネットは環境により異なります。`ip -4 addr` で自分のアドレスを確認してから設定してください。
> **SSH の許可を忘れるとリモートから復旧できなくなります。**

### 外出先からアクセスしたくなったら

ポート開放ではなく Tailscale などの VPN を使ってください。ポート開放は行わない前提の構成です。

---

## トラブルシュート

### API が起動しない

```bash
sudo journalctl -u menucraft-api -n 80 --no-pager
```

| ログの内容 | 原因と対処 |
|---|---|
| `JWT secret not configured` | `Jwt__Secret` が未設定。`/etc/menucraft/menucraft.env` を確認 |
| `unable to open database file` | `/var/lib/menucraft` の所有者を確認: `sudo chown -R menucraft:menucraft /var/lib/menucraft` |
| `Database:Provider の値 ... は不正です` | `Database__Provider` は `Sqlite` か `SqlServer` のみ |
| `Address already in use` | 7071 番ポートが使用中。`sudo ss -tlnp \| grep 7071` |
| `Read-only file system : '/opt/menucraft/.azurefunctions'` | Core Tools の `HOME` が読み取り専用。下記参照 |
| `dotnet exited with code 150` / `Failed to start language worker process` | 必要な .NET ランタイムが未インストール。下記参照 |

#### `dotnet exited with code 150` で worker が起動しない

`Microsoft.Azure.WebJobs.Script.Grpc: dotnet exited with code 150 (0x96)` は、
isolated worker がマネージドコードに入る前に「要求するフレームワークが見つからない」で
終了したことを意味します。ホストが worker の標準エラーを握り潰すため、
journal には本当の理由が出ません。

要求バージョンとインストール済みバージョンを比べます。

```bash
# アプリが要求するバージョン
grep -A2 '"framework"' /opt/menucraft/api/MenuCraft.Api.runtimeconfig.json

# インストール済みのランタイム
/usr/share/dotnet/dotnet --list-runtimes
```

`Microsoft.NETCore.App` の要求バージョン系列（例: `10.0.0` なら 10.x）が一覧に無ければ原因確定です。
.NET は既定でメジャーバージョンを跨いでロールフォワードしないため、
.NET 8 だけが入っている環境では net10.0 のアプリは動きません（逆も同じ）。

```bash
# setup.sh が side-by-side で追加インストールします（既存の .NET はそのまま）
sudo ./deploy/raspi/setup.sh

# 手動で入れる場合（バージョンは runtimeconfig.json の要求に合わせる）
curl -fsSL https://dot.net/v1/dotnet-install.sh \
  | sudo bash -s -- --channel 10.0 --runtime dotnet --install-dir /usr/share/dotnet
sudo systemctl restart menucraft-api
```

#### `Read-only file system : '/opt/menucraft/.azurefunctions'` で起動ループする

Core Tools は起動時に `$HOME/.azurefunctions` を作成します。`menucraft` ユーザーの
既定ホームは `/opt/menucraft` ですが、`menucraft-api.service` の `ProtectSystem=strict`
により `/opt` は読み取り専用のため、書き込みに失敗して `status=6/ABRT` で再起動を繰り返します。

現在のユニットは `HOME=/var/lib/menucraft/home` を指定してこれを回避します。
古いユニットのまま動いている場合は最新のリポジトリで再デプロイしてください。

```bash
cd ~/MenuCraft && git pull
sudo ./deploy/raspi/deploy.sh
# または初回セットアップからやり直す場合
sudo ./deploy/raspi/setup.sh
```

手動で確認する場合:

```bash
systemctl show menucraft-api -p Environment | tr ' ' '\n' | grep HOME
# → HOME=/var/lib/menucraft/home であること
ls -ld /var/lib/menucraft/home   # menucraft:menucraft 所有であること
```

### ブラウザから開けない

```bash
# nginx の状態と設定
sudo systemctl status nginx
sudo nginx -t

# ラズパイ自身からは見えるか
curl http://127.0.0.1/api/health

# ラズパイの IP を確認
hostname -I
```

`menucraft.local` で解決できない場合は IP を直接指定してください。
Windows から `.local` を引けないことがあります（Bonjour 未導入の場合）。

### 画面は出るが API 呼び出しが失敗する

`/var/www/menucraft/js/config.js` の `API_BASE_URL` が `''` になっているか確認します。

```bash
grep API_BASE_URL /var/www/menucraft/js/config.js
# → API_BASE_URL: '',  であること
# → '__API_BASE_URL__' のままなら sudo ./deploy/raspi/deploy.sh を再実行
```

### 動作が遅い

初回アクセスは .NET のコールドスタートで数秒かかります。2 回目以降は速くなります。
恒常的に遅い場合はメモリ不足を疑ってください（`free -h`）。

---

## アンインストール

```bash
sudo systemctl disable --now menucraft-api menucraft-backup.timer
sudo rm -f /etc/systemd/system/menucraft-*.service /etc/systemd/system/menucraft-*.timer
sudo rm -f /etc/systemd/journald.conf.d/menucraft.conf
sudo systemctl daemon-reload

sudo rm -f /etc/nginx/sites-enabled/menucraft /etc/nginx/sites-available/menucraft
sudo systemctl reload nginx

sudo rm -rf /opt/menucraft /var/www/menucraft /etc/menucraft
# データを消してよい場合のみ
sudo rm -rf /var/lib/menucraft /var/backups/menucraft
sudo userdel menucraft
```

---

## Azure 版との併存について

Azure 用の資産（`infra/` の Bicep、`deploy-api.yml` / `deploy-frontend.yml`）はそのまま残しています。
ただし**コミット時の自動デプロイは無効化**されており、GitHub Actions の
「Run workflow」から手動実行したときのみ Azure にデプロイされます。

push / PR 時には `ci.yml`（ビルド・テスト・shellcheck）のみが動きます。

API のコードは `Database__Provider` の設定だけで両環境に対応します。
Azure 側では設定を省略すれば従来どおり SQL Server が使われます。
