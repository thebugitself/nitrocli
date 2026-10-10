#!/usr/bin/env bash
# Remove nitroctl modules + CLI + boot config.
set -euo pipefail

KVER="$(uname -r)"
EXTRA_DIR="/lib/modules/${KVER}/extra"

if [[ $EUID -ne 0 ]]; then
    echo "run as root:  sudo $0" >&2
    exit 1
fi

modprobe -r acer-wmi-rgb 2>/dev/null || true
modprobe -r acer-wmi-battery 2>/dev/null || true

rm -f "${EXTRA_DIR}/acer-wmi-battery.ko" "${EXTRA_DIR}/acer-wmi-rgb.ko"
rm -f /usr/local/bin/nitroctl
rm -f /etc/modules-load.d/acer-nitro.conf
rm -f /etc/modprobe.d/acer-wmi.conf

depmod -a "${KVER}"
echo "removed."
