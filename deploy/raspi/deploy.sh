#!/usr/bin/env bash
#
# MenuCraft — 更新デプロイ（2 回目以降）
#
#   sudo ./deploy/raspi/deploy.sh          # 現在のワーキングツリーをデプロイ
#   sudo ./deploy/raspi/deploy.sh --pull   # git pull してからデプロイ
#
# 初回セットアップは setup.sh を使うこと。
# このスクリプトは既存の /etc/menucraft/menucraft.env と DB を変更しない。

set -euo pipefail

APP_USER="menucraft"
APP_GROUP="menucraft"
INSTALL_ROOT="/opt/menucraft"
API_DIR="${INSTALL_ROOT}/api"
WEB_ROOT="/var/www/menucraft"
DATA_DIR="/var/lib/menucraft"
# Core Tools 用の書き込み可能な HOME (menucraft-api.service の Environment=HOME=)
RUNTIME_HOME="${DATA_DIR}/home"
ENV_FILE="/etc/menucraft/menucraft.env"
DOTNET_ROOT_DIR="/usr/share/dotnet"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ASSETS_DIR="${REPO_ROOT}/deploy/raspi"

DO_PULL=0
[[ "${1:-}" == "--pull" ]] && DO_PULL=1

log()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m警告:\033[0m %s\n' "$*" >&2; }
die()  { printf '\033[1;31mエラー:\033[0m %s\n' "$*" >&2; exit 1; }

[[ ${EUID} -eq 0 ]] || die "root で実行してください: sudo $0"
[[ -f "${ENV_FILE}" ]] || die "${ENV_FILE} がありません。先に setup.sh を実行してください。"
[[ -x "${DOTNET_ROOT_DIR}/dotnet" ]] || die ".NET が見つかりません。先に setup.sh を実行してください。"

if [[ ${DO_PULL} -eq 1 ]]; then
  log "最新のコードを取得しています"
  git -C "${REPO_ROOT}" pull --ff-only || die "git pull に失敗しました"
fi

log "現在のリビジョン: $(git -C "${REPO_ROOT}" rev-parse --short HEAD 2>/dev/null || echo unknown)"

log "API をビルドしています"
BUILD_DIR="$(mktemp -d)"
trap 'rm -rf "${BUILD_DIR}"' EXIT

DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
  "${DOTNET_ROOT_DIR}/dotnet" publish "${REPO_ROOT}/src/api/" \
    --configuration Release \
    --output "${BUILD_DIR}" \
  || die "ビルドに失敗しました。既存のサービスは停止していません。"

# ビルド成功後にはじめてサービスを止める（失敗時のダウンタイムを避ける）
log "menucraft-api を停止しています"
systemctl stop menucraft-api.service

log "API を差し替えています"
rm -rf "${API_DIR:?}"/*
cp -a "${BUILD_DIR}/." "${API_DIR}/"
rm -f "${API_DIR}/local.settings.json"
chown -R "${APP_USER}:${APP_GROUP}" "${API_DIR}"

log "フロントエンドを差し替えています"
rsync -a --delete --exclude '__tests__' "${REPO_ROOT}/src/client/" "${WEB_ROOT}/"
sed -i "s|__API_BASE_URL__||g" "${WEB_ROOT}/js/config.js"
grep -q "__API_BASE_URL__" "${WEB_ROOT}/js/config.js" \
  && die "config.js の API_BASE_URL 置換に失敗しました"
chown -R www-data:www-data "${WEB_ROOT}"
find "${WEB_ROOT}" -type d -exec chmod 755 {} +
find "${WEB_ROOT}" -type f -exec chmod 644 {} +

# 旧バージョンからの更新でも Core Tools の HOME を確実に用意する
mkdir -p "${RUNTIME_HOME}"
chown "${APP_USER}:${APP_GROUP}" "${RUNTIME_HOME}"
chmod 700 "${RUNTIME_HOME}"

# systemd / nginx の設定に変更があれば取り込む
log "サービス定義を同期しています"
install -m 644 "${ASSETS_DIR}/menucraft-api.service"    /etc/systemd/system/
install -m 644 "${ASSETS_DIR}/menucraft-backup.service" /etc/systemd/system/
install -m 644 "${ASSETS_DIR}/menucraft-backup.timer"   /etc/systemd/system/
install -m 755 "${ASSETS_DIR}/backup.sh"                "${INSTALL_ROOT}/backup.sh"
install -m 644 "${ASSETS_DIR}/nginx-menucraft.conf"     /etc/nginx/sites-available/menucraft
systemctl daemon-reload
nginx -t || die "nginx の設定が不正です"

log "サービスを起動しています"
systemctl start menucraft-api.service
systemctl reload nginx

log "ヘルスチェックを実行しています"
for _ in $(seq 1 30); do
  if curl -fsS -m 5 http://127.0.0.1/api/health >/dev/null 2>&1; then
    log "デプロイが完了しました — http://$(hostname).local/"
    exit 0
  fi
  sleep 2
done

warn "ヘルスチェックに失敗しました。直近のログを表示します:"
journalctl -u menucraft-api -n 40 --no-pager >&2 || true
warn "続きのログ: sudo journalctl -u menucraft-api -n 100 --no-pager"
exit 1
