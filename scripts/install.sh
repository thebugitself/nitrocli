#!/usr/bin/env bash
# Install nitroctl: install the out-of-tree modules for the running kernel,
# install the CLI, and set up boot-time module loading.
#
# Works in two modes:
#   1. build from source, if matching kernel headers are present
#   2. install the prebuilt kernel/*.ko (already built against uname -r)
#      if the headers for the running kernel are not installed
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
KVER="$(uname -r)"
EXTRA_DIR="/lib/modules/${KVER}/extra"
BINDIR="/usr/local/bin"
BUILD="/lib/modules/${KVER}/build"

if [[ $EUID -ne 0 ]]; then
    echo "run as root:  sudo $0" >&2
    exit 1
fi

have_prebuilt=0
if compgen -G "${ROOT}/kernel/*.ko" >/dev/null; then
    have_prebuilt=1
fi

if [[ -d "$BUILD" ]]; then
    echo "==> building modules (headers: $BUILD)"
    make -C "${ROOT}/kernel" clean >/dev/null 2>&1 || true
    make -C "${ROOT}/kernel"
else
    cat >&2 <<EOF
warning: kernel headers for ${KVER} are not installed at ${BUILD}
         (this happens when the installed headers are a different version
          than the running kernel - common on Arch).
EOF
    if [[ $have_prebuilt -eq 1 ]]; then
        echo "==> using the prebuilt kernel/*.ko instead"
    else
        cat >&2 <<EOF
error: no headers and no prebuilt modules.
Install matching headers and retry, e.g.:
   Arch:   the headers package must match: uname -r == $(uname -r)
           (if unavailable, fetch matching linux-zen-headers from
            archive.archlinux.org and run the build with KDIR=<path>)
   Debian: sudo apt install linux-headers-\$(uname -r)
EOF
        exit 1
    fi
fi

if ! compgen -G "${ROOT}/kernel/*.ko" >/dev/null; then
    echo "error: build produced no .ko files" >&2
    exit 1
fi

echo "==> installing modules to ${EXTRA_DIR}"
install -d "${EXTRA_DIR}"
install -m 0644 "${ROOT}"/kernel/*.ko "${EXTRA_DIR}/"
depmod -a "${KVER}"

echo "==> installing CLI to ${BINDIR}/nitroctl"
install -d "${BINDIR}"
install -m 0755 "${ROOT}/cli/nitroctl" "${BINDIR}/nitroctl"

echo "==> installing modprobe.d + modules-load.d + udev rule"
install -m 0644 "${ROOT}/scripts/modules-load.d/acer-nitro.conf" \
    /etc/modules-load.d/acer-nitro.conf
install -m 0644 "${ROOT}/scripts/modprobe.d/acer-wmi.conf" \
    /etc/modprobe.d/acer-wmi.conf
install -m 0644 "${ROOT}/scripts/udev/60-acer-nitro-rgb.rules" \
    /etc/udev/rules.d/60-acer-nitro-rgb.rules
udevadm control --reload-rules 2>/dev/null || true
udevadm trigger --subsystem-match=wmi 2>/dev/null || true

echo "==> loading modules now"
modprobe acer-wmi-battery || true
modprobe acer-wmi-rgb || true

echo
echo "done. 'sudo nitroctl ...' now works. try:"
echo "  nitroctl doctor"
echo "  sudo nitroctl battery limit on"
