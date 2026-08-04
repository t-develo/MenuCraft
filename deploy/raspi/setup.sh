#!/usr/bin/env bash
#
# MenuCraft — Raspberry Pi 初回セットアップ
#
#   sudo ./deploy/raspi/setup.sh
#
# 冪等: 何度実行しても安全。既存の設定ファイル・DB は上書きしない。
# 日常の更新は deploy.sh を使うこと。

set -euo pipefail

# ---------------------------------------------------------------------------
# 設定
# ---------------------------------------------------------------------------
CORE_TOOLS_VERSION="4.12.1"
DOTNET_CHANNEL="8.0"

APP_USER="menucraft"
APP_GROUP="menucraft"

INSTALL_ROOT="/opt/menucraft"
API_DIR="${INSTALL_ROOT}/api"
CORE_TOOLS_DIR="${INSTALL_ROOT}/core-tools"
WEB_ROOT="/var/www/menucraft"
DATA_DIR="/var/lib/menucraft"
CONFIG_DIR="/etc/menucraft"
ENV_FILE="${CONFIG_DIR}/menucraft.env"
BACKUP_DIR="/var/backups/menucraft"

DOTNET_ROOT_DIR="/usr/share/dotnet"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ASSETS_DIR="${REPO_ROOT}/deploy/raspi"

# ---------------------------------------------------------------------------
# ログ出力
# ---------------------------------------------------------------------------
log()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m警告:\033[0m %s\n' "$*" >&2; }
die()  { printf '\033[1;31mエラー:\033[0m %s\n' "$*" >&2; exit 1; }

# ---------------------------------------------------------------------------
# 1. 事前チェック
# ---------------------------------------------------------------------------
preflight() {
  log "実行環境を確認しています"

  [[ ${EUID} -eq 0 ]] || die "root で実行してください: sudo $0"

  local arch
  arch="$(uname -m)"
  if [[ "${arch}" != "aarch64" && "${arch}" != "x86_64" ]]; then
    die "アーキテクチャ ${arch} は非対応です。
.NET 8 は 32bit ARM (armv7l) をサポートしていません。
Raspberry Pi OS の 64bit 版 (arm64) を使用してください。
確認: uname -m の結果が aarch64 になっている必要があります。"
  fi

  command -v systemctl >/dev/null 2>&1 || die "systemd が見つかりません。このスクリプトは systemd 環境が前提です。"
  command -v apt-get   >/dev/null 2>&1 || die "apt-get が見つかりません。Debian / Raspberry Pi OS / Ubuntu が前提です。"

  [[ -d "${ASSETS_DIR}" ]] || die "リポジトリ内の deploy/raspi が見つかりません: ${ASSETS_DIR}"
  [[ -d "${REPO_ROOT}/src/api"    ]] || die "src/api が見つかりません。リポジトリのルートから実行してください。"
  [[ -d "${REPO_ROOT}/src/client" ]] || die "src/client が見つかりません。リポジトリのルートから実行してください。"

  log "アーキテクチャ: ${arch} — OK"
}

# ---------------------------------------------------------------------------
# 2. OS パッケージ
# ---------------------------------------------------------------------------
install_packages() {
  log "APT パッケージをインストールしています"
  apt-get update -qq
  apt-get install -y --no-install-recommends \
    nginx sqlite3 curl unzip rsync ca-certificates openssl
}

# ---------------------------------------------------------------------------
# 3. .NET SDK
# ---------------------------------------------------------------------------
install_dotnet() {
  if [[ -x "${DOTNET_ROOT_DIR}/dotnet" ]]; then
    log ".NET は導入済みです ($("${DOTNET_ROOT_DIR}/dotnet" --version))"
    return
  fi

  log ".NET ${DOTNET_CHANNEL} SDK をインストールしています（数分かかります）"
  local installer
  installer="$(mktemp)"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${installer}"
  chmod +x "${installer}"
  "${installer}" --channel "${DOTNET_CHANNEL}" --install-dir "${DOTNET_ROOT_DIR}"
  rm -f "${installer}"

  ln -sf "${DOTNET_ROOT_DIR}/dotnet" /usr/local/bin/dotnet
  log ".NET $("${DOTNET_ROOT_DIR}/dotnet" --version) をインストールしました"
}

# ---------------------------------------------------------------------------
# 4. Azure Functions Core Tools
# ---------------------------------------------------------------------------
install_core_tools() {
  if [[ -x "${CORE_TOOLS_DIR}/func" ]]; then
    local current
    current="$("${CORE_TOOLS_DIR}/func" --version 2>/dev/null || echo "unknown")"
    if [[ "${current}" == "${CORE_TOOLS_VERSION}" ]]; then
      log "Azure Functions Core Tools ${CORE_TOOLS_VERSION} は導入済みです"
      return
    fi
    log "Core Tools を ${current} から ${CORE_TOOLS_VERSION} へ更新します"
    rm -rf "${CORE_TOOLS_DIR}"
  fi

  local rid
  case "$(uname -m)" in
    aarch64) rid="linux-arm64" ;;
    x86_64)  rid="linux-x64"   ;;
    *)       die "対応していないアーキテクチャです: $(uname -m)" ;;
  esac

  local url="https://github.com/Azure/azure-functions-core-tools/releases/download/${CORE_TOOLS_VERSION}/Azure.Functions.Cli.${rid}.${CORE_TOOLS_VERSION}.zip"
  log "Azure Functions Core Tools ${CORE_TOOLS_VERSION} (${rid}) を取得しています"

  local archive
  archive="$(mktemp --suffix=.zip)"
  curl -fSL --retry 3 -o "${archive}" "${url}" \
    || die "Core Tools のダウンロードに失敗しました: ${url}"

  mkdir -p "${CORE_TOOLS_DIR}"
  unzip -q -o "${archive}" -d "${CORE_TOOLS_DIR}"
  rm -f "${archive}"

  chmod +x "${CORE_TOOLS_DIR}/func" "${CORE_TOOLS_DIR}/gozip" 2>/dev/null || true
  [[ -x "${CORE_TOOLS_DIR}/func" ]] || die "Core Tools の展開に失敗しました"

  log "Core Tools $("${CORE_TOOLS_DIR}/func" --version) をインストールしました"
}

# ---------------------------------------------------------------------------
# 5. 実行ユーザーとディレクトリ
# ---------------------------------------------------------------------------
create_user_and_dirs() {
  if ! getent group "${APP_GROUP}" >/dev/null; then
    groupadd --system "${APP_GROUP}"
    log "グループ ${APP_GROUP} を作成しました"
  fi

  if ! id -u "${APP_USER}" >/dev/null 2>&1; then
    useradd --system --gid "${APP_GROUP}" --home-dir "${INSTALL_ROOT}" \
            --shell /usr/sbin/nologin "${APP_USER}"
    log "ユーザー ${APP_USER} を作成しました"
  fi

  mkdir -p "${API_DIR}" "${CORE_TOOLS_DIR}" "${WEB_ROOT}" "${DATA_DIR}" "${CONFIG_DIR}" "${BACKUP_DIR}"

  chown -R "${APP_USER}:${APP_GROUP}" "${INSTALL_ROOT}" "${DATA_DIR}" "${BACKUP_DIR}"
  chmod 750 "${DATA_DIR}" "${BACKUP_DIR}"

  chown root:"${APP_GROUP}" "${CONFIG_DIR}"
  chmod 750 "${CONFIG_DIR}"
}

# ---------------------------------------------------------------------------
# 6. 環境変数ファイル
# ---------------------------------------------------------------------------
create_env_file() {
  if [[ -f "${ENV_FILE}" ]]; then
    log "${ENV_FILE} は既に存在するため、そのまま使用します"
    return
  fi

  log "${ENV_FILE} を生成しています"

  local jwt_secret
  jwt_secret="$(openssl rand -base64 48 | tr -d '\n')"

  # sed の置換文字列に含まれうる区切り文字を避けるため | を使う
  sed "s|__REPLACE_WITH_RANDOM_SECRET__|${jwt_secret}|" \
    "${ASSETS_DIR}/menucraft.env.example" > "${ENV_FILE}"

  chown root:"${APP_GROUP}" "${ENV_FILE}"
  chmod 640 "${ENV_FILE}"

  log "JWT シークレットを生成しました（${ENV_FILE} に保存済み）"
}

# ---------------------------------------------------------------------------
# 7. API のビルドと配置
# ---------------------------------------------------------------------------
publish_api() {
  log "API をビルドしています（初回は数分かかります）"

  # ビルド成果物は root 所有で作られるため、あとでまとめて chown する
  rm -rf "${API_DIR:?}"/*
  DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
    "${DOTNET_ROOT_DIR}/dotnet" publish "${REPO_ROOT}/src/api/" \
      --configuration Release \
      --output "${API_DIR}" \
    || die "API のビルドに失敗しました"

  # local.settings.json は環境変数で置き換えるため配置しない
  rm -f "${API_DIR}/local.settings.json"

  chown -R "${APP_USER}:${APP_GROUP}" "${API_DIR}"
  log "API を ${API_DIR} に配置しました"
}

# ---------------------------------------------------------------------------
# 8. フロントエンドの配置
# ---------------------------------------------------------------------------
deploy_client() {
  log "フロントエンドを ${WEB_ROOT} に配置しています"

  rsync -a --delete \
    --exclude '__tests__' \
    "${REPO_ROOT}/src/client/" "${WEB_ROOT}/"

  # nginx が /api/ を同一オリジンで中継するため API_BASE_URL は空にする
  sed -i "s|__API_BASE_URL__||g" "${WEB_ROOT}/js/config.js"

  if grep -q "__API_BASE_URL__" "${WEB_ROOT}/js/config.js"; then
    die "config.js の API_BASE_URL 置換に失敗しました"
  fi

  chown -R www-data:www-data "${WEB_ROOT}"
  find "${WEB_ROOT}" -type d -exec chmod 755 {} +
  find "${WEB_ROOT}" -type f -exec chmod 644 {} +
}

# ---------------------------------------------------------------------------
# 9. systemd / nginx / journald
# ---------------------------------------------------------------------------
install_services() {
  log "systemd ユニットと nginx 設定を配置しています"

  install -m 644 "${ASSETS_DIR}/menucraft-api.service"     /etc/systemd/system/
  install -m 644 "${ASSETS_DIR}/menucraft-backup.service"  /etc/systemd/system/
  install -m 644 "${ASSETS_DIR}/menucraft-backup.timer"    /etc/systemd/system/
  install -m 755 "${ASSETS_DIR}/backup.sh"                 "${INSTALL_ROOT}/backup.sh"

  # SD カードの書き込み寿命対策: journald のログ上限を抑える
  mkdir -p /etc/systemd/journald.conf.d
  cat > /etc/systemd/journald.conf.d/menucraft.conf <<'EOF'
# MenuCraft: SD カードの書き込み量を抑えるためログ保持量を制限する
[Journal]
SystemMaxUse=200M
SystemMaxFileSize=20M
EOF
  systemctl restart systemd-journald

  install -m 644 "${ASSETS_DIR}/nginx-menucraft.conf" /etc/nginx/sites-available/menucraft

  # IPv6 が無効な環境では listen [::]:80 が起動時エラーになるため取り除く
  if [[ ! -f /proc/net/if_inet6 ]]; then
    warn "IPv6 が無効なため、nginx の IPv6 リスナーを無効化します"
    sed -i 's|^\( *\)listen \[::\]:80 default_server;|\1# listen [::]:80 default_server;  # IPv6 無効のため setup.sh がコメントアウト|' \
      /etc/nginx/sites-available/menucraft
  fi

  ln -sf /etc/nginx/sites-available/menucraft /etc/nginx/sites-enabled/menucraft
  # Debian 既定のサイトは default_server が衝突するため無効化する
  rm -f /etc/nginx/sites-enabled/default

  nginx -t || die "nginx の設定が不正です"

  systemctl daemon-reload
  systemctl enable --now nginx
  systemctl enable --now menucraft-api.service
  systemctl enable --now menucraft-backup.timer

  systemctl restart menucraft-api.service
  systemctl reload nginx
}

# ---------------------------------------------------------------------------
# 10. 動作確認
# ---------------------------------------------------------------------------
verify() {
  log "起動を待っています"

  local ok=0
  for _ in $(seq 1 30); do
    if curl -fsS -m 5 http://127.0.0.1:7071/api/health >/dev/null 2>&1; then
      ok=1
      break
    fi
    sleep 2
  done

  if [[ ${ok} -ne 1 ]]; then
    warn "API のヘルスチェックに失敗しました。ログを確認してください:"
    warn "  sudo journalctl -u menucraft-api -n 50 --no-pager"
    die "セットアップは完了しませんでした"
  fi
  log "API 直接アクセス (127.0.0.1:7071/api/health) — OK"

  curl -fsS -m 5 http://127.0.0.1/api/health >/dev/null \
    || die "nginx 経由の /api/health に失敗しました。nginx のログを確認してください: sudo journalctl -u nginx -n 50"
  log "nginx 経由 (127.0.0.1/api/health) — OK"

  curl -fsS -m 5 -o /dev/null http://127.0.0.1/ \
    || die "フロントエンドの配信に失敗しました"
  log "フロントエンド配信 — OK"
}

print_summary() {
  local host_ip
  host_ip="$(hostname -I 2>/dev/null | awk '{print $1}')"

  cat <<EOF

============================================================
 MenuCraft のセットアップが完了しました
============================================================

 アクセス URL:
   http://$(hostname).local/
   http://${host_ip:-<ラズパイのIP>}/

 最初のユーザー登録:
   ブラウザで上記 URL を開き「新規登録」してください。
   最初に登録したユーザーが自動的に管理者 (Admin) になります。

 よく使うコマンド:
   状態確認   sudo systemctl status menucraft-api
   ログ確認   sudo journalctl -u menucraft-api -f
   再起動     sudo systemctl restart menucraft-api
   更新       sudo ./deploy/raspi/deploy.sh
   バックアップ手動実行  sudo systemctl start menucraft-backup.service

 データベース : ${DATA_DIR}/menucraft.db
 バックアップ : ${BACKUP_DIR}
 設定ファイル : ${ENV_FILE}

 詳細は docs/RASPBERRY_PI.md を参照してください。
============================================================

EOF
}

# ---------------------------------------------------------------------------
main() {
  preflight
  install_packages
  create_user_and_dirs
  install_dotnet
  install_core_tools
  create_env_file
  publish_api
  deploy_client
  install_services
  verify
  print_summary
}

main "$@"
