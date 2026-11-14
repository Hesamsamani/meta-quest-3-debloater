#!/usr/bin/env bash
# Restore all disabled Meta Quest catalog packages
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CATALOG="$ROOT/configs/packages-catalog.json"

command -v adb >/dev/null || { echo "adb not found in PATH" >&2; exit 1; }
SERIAL="$(adb devices | awk '$2=="device"{print $1; exit}')"
[ -n "$SERIAL" ] || { echo "No authorized Quest device found via adb." >&2; exit 1; }

DISABLED=$(adb -s "$SERIAL" shell pm list packages -d | tr -d '\r' | sed 's/^package://')
PKGS=$(python3 - "$CATALOG" <<'PY'
import json, sys
cat = json.load(open(sys.argv[1], encoding="utf-8"))
for p in cat["packages"]:
    if p.get("tier") != "PROTECTED":
        print(p["package"])
PY
)

for p in $PKGS; do
  echo "$DISABLED" | grep -qx "$p" || continue
  adb -s "$SERIAL" shell "pm enable $p" | tr -d '\r'
done

echo "All catalog packages re-enabled on $SERIAL."
