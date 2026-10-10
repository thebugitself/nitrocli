#!/usr/bin/env bash
# Load the RGB driver and prove it talks to the EC: read -> set -> read back.
# Idempotent: safe to re-run when the modules are already loaded.
# Run with:  sudo ./scripts/verify-rgb.sh [RRGGBB]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COLOR="${1:-00ff00}"
DIR=/sys/bus/wmi/drivers/acer-wmi-rgb

echo "== loading modules =="
if lsmod | grep -q '^acer_wmi_battery'; then
    echo "  acer-wmi-battery already loaded"
else
    modprobe acer-wmi-battery 2>/dev/null || insmod "${ROOT}/kernel/acer-wmi-battery.ko"
fi
if lsmod | grep -q '^acer_wmi_rgb'; then
    echo "  acer-wmi-rgb already loaded"
else
    modprobe acer-wmi-rgb 2>/dev/null || insmod "${ROOT}/kernel/acer-wmi-rgb.ko"
fi

if [[ ! -d "$DIR" ]]; then
    echo "error: $DIR does not exist - driver failed to bind" >&2
    echo "(does this machine expose WMI guid 7A4DDFE7-...?)" >&2
    exit 1
fi

echo "== bound WMI device =="
for d in /sys/bus/wmi/devices/7A4DDFE7-*; do
    [ -e "$d" ] && echo "  $(basename "$d")"
done

echo "== zones BEFORE (read from EC) =="
for z in zone1 zone2 zone3 zone4; do
    printf '  %-8s #%s\n' "$z" "$(cat "$DIR/$z")"
done

echo "== setting all zones -> #$COLOR =="
for z in zone1 zone2 zone3 zone4; do printf '%s' "$COLOR" > "$DIR/$z"; done

echo "== zones AFTER =="
ok=1
for z in zone1 zone2 zone3 zone4; do
    got="$(cat "$DIR/$z")"
    printf '  %-8s #%s\n' "$z" "$got"
    [[ "$got" == "${COLOR,,}" ]] || ok=0
done

echo
if [[ $ok -eq 1 ]]; then
    echo "SUCCESS: EC stored #$COLOR for all 4 zones - the keyboard should have changed colour."
    echo "(per-zone: 'nitroctl rgb set ff0000 -z 1,2'  |  off: 'nitroctl rgb off')"
else
    echo "MISMATCH: EC did not store the value. The write path may need fixing - send this output."
fi
