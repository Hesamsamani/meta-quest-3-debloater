#!/usr/bin/env bash
# Reversible Meta Quest 3 debloat script (macOS / Linux / Git Bash / WSL)
# Usage: ./scripts/debloat.sh [telemetry|safe|pcvr]
set -euo pipefail

PRESET="${1:-safe}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CATALOG="$ROOT/configs/packages-catalog.json"
BACKUP_DIR="$ROOT/backups"
mkdir -p "$BACKUP_DIR"

command -v adb >/dev/null || { echo "adb not found in PATH" >&2; exit 1; }
SERIAL="$(adb devices | awk '$2=="device"{print $1; exit}')"
[ -n "$SERIAL" ] || { echo "No authorized Quest device found via adb." >&2; exit 1; }

BEFORE="$BACKUP_DIR/quest3-${SERIAL}-before.txt"
[ -e "$BEFORE" ] || adb -s "$SERIAL" shell pm list packages -d | tr -d '\r' > "$BEFORE"

PKGS=$(python3 - "$CATALOG" "$PRESET" <<'PY'
import json, sys
cat = json.load(open(sys.argv[1], encoding="utf-8"))
preset = sys.argv[2]
for p in cat["packages"]:
    if p.get("tier") != "PROTECTED" and preset in p.get("presets", []):
        print(p["package"])
PY
)

INSTALLED=$(adb -s "$SERIAL" shell pm list packages | tr -d '\r' | sed 's/^package://')
for p in $PKGS; do
  echo "$INSTALLED" | grep -qx "$p" || continue
  adb -s "$SERIAL" shell "pm disable-user --user 0 $p && am force-stop $p" | tr -d '\r'
  home=$(adb -s "$SERIAL" shell "cmd package resolve-activity --brief -c android.intent.category.HOME -a android.intent.action.MAIN" | tr -d '\r' | tail -n 1)
  case "$home" in
    com.oculus.vrshell/*) ;;
    *) echo "SAFETY ROLLBACK after $p (HOME='$home')" >&2; adb -s "$SERIAL" shell "pm enable $p"; exit 1 ;;
  esac
done

echo "Done! Undo anytime with: ./scripts/restore.sh"
