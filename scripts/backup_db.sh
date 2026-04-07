#!/usr/bin/env bash
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-./backups}"
PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGDATABASE="${PGDATABASE:-KodvianSuperMarket}"
PGUSER="${PGUSER:-postgres}"

mkdir -p "$BACKUP_DIR"

STAMP="$(date +%Y%m%d_%H%M%S)"
FILE="$BACKUP_DIR/${PGDATABASE}_${STAMP}.dump"

pg_dump -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -Fc "$PGDATABASE" > "$FILE"

find "$BACKUP_DIR" -type f -name "${PGDATABASE}_*.dump" -mtime +14 -delete

LAST_BACKUP_FILE="$BACKUP_DIR/last_backup.txt"
printf '{"timestamp":"%s","file":"%s"}\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$(basename "$FILE")" > "$LAST_BACKUP_FILE"

echo "Backup created: $FILE"
