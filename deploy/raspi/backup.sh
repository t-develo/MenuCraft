#!/usr/bin/env bash
#
# MenuCraft — SQLite データベースのバックアップ
#
# menucraft-backup.timer から毎日呼び出される。手動実行も可能:
#   sudo systemctl start menucraft-backup.service
#   sudo /opt/menucraft/backup.sh
#
# 単純なファイルコピーは使わない。WAL モードでは直近の書き込みが
# menucraft.db-wal 側にあり、.db だけコピーするとデータを取りこぼすため、
# sqlite3 の .backup コマンド（オンラインバックアップ API）を使用する。

set -euo pipefail

DB_PATH="${MENUCRAFT_DB_PATH:-/var/lib/menucraft/menucraft.db}"
BACKUP_DIR="${MENUCRAFT_BACKUP_DIR:-/var/backups/menucraft}"
RETENTION="${MENUCRAFT_BACKUP_RETENTION:-14}"

log() { printf '%s [backup] %s\n' "$(date -Is)" "$*"; }
die() { printf '%s [backup] エラー: %s\n' "$(date -Is)" "$*" >&2; exit 1; }

command -v sqlite3 >/dev/null 2>&1 || die "sqlite3 コマンドが見つかりません"
[[ -f "${DB_PATH}" ]] || die "データベースが見つかりません: ${DB_PATH}"

mkdir -p "${BACKUP_DIR}"

TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
TARGET="${BACKUP_DIR}/menucraft-${TIMESTAMP}.db"

log "バックアップを作成しています: ${TARGET}"
sqlite3 "${DB_PATH}" ".backup '${TARGET}'" || die "バックアップの作成に失敗しました"

# 破損したバックアップを残さないよう整合性を検証する
RESULT="$(sqlite3 "${TARGET}" "PRAGMA integrity_check;" 2>/dev/null || echo "failed")"
if [[ "${RESULT}" != "ok" ]]; then
  rm -f "${TARGET}"
  die "整合性チェックに失敗したためバックアップを破棄しました: ${RESULT}"
fi

gzip -f "${TARGET}"
log "バックアップ完了: ${TARGET}.gz ($(du -h "${TARGET}.gz" | cut -f1))"

# 古い世代を削除（新しい順に RETENTION 個を残す）
mapfile -t OLD < <(find "${BACKUP_DIR}" -maxdepth 1 -name 'menucraft-*.db.gz' -type f -printf '%T@ %p\n' \
  | sort -rn | tail -n "+$((RETENTION + 1))" | cut -d' ' -f2-)

if [[ ${#OLD[@]} -gt 0 ]]; then
  for f in "${OLD[@]}"; do
    rm -f "${f}"
    log "古いバックアップを削除しました: $(basename "${f}")"
  done
fi

log "保持世代数: $(find "${BACKUP_DIR}" -maxdepth 1 -name 'menucraft-*.db.gz' -type f | wc -l) / ${RETENTION}"
