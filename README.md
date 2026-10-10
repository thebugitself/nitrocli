# nitroctl

**Acer Care Center + NitroSense features for Linux, from the terminal.**

`nitroctl` re-implements the useful parts of Acer's Windows utilities —
**Acer Care Center** (battery charge limiter / calibration) and **NitroSense**
(4-zone keyboard RGB, performance profile, fan & temperature monitoring) — as a
small CLI for Acer Nitro / Predator laptops on Linux.

Developed and verified on an **Acer Nitro 5 AN515-58** (`Jimny_ADH`),
Arch Linux, kernel `7.2.6-zen2-1-zen`, Wayland/Hyprland.

---

## Features

| Feature | Windows app it comes from | Linux backend | Status |
|---|---|---|---|
| **Battery charge limit** (stop charging at ~80%) | Care Center | `acer-wmi-battery` (this repo) | ✅ verified |
| **Battery calibration** (charge/discharge cycle) | Care Center | `acer-wmi-battery` | ✅ verified |
| **Battery temperature** | Care Center | `acer-wmi-battery` | ✅ verified |
| **4-zone keyboard RGB** | NitroSense | `acer-wmi-rgb` (this repo) | ✅ verified (write + read-back via EC) |
| **Performance profile** (quiet/balanced/performance) | NitroSense | mainline `acer-wmi` → `platform-profile` | ✅ verified |
| **Fan RPM + temperatures** | NitroSense | mainline `acer-wmi` → `hwmon` | ✅ verified |
| **Manual fan speed** | NitroSense | mainline `acer-wmi` → `hwmon` `pwm1/2` | ✅ verified (overrides firmware; needs `fan auto` to restore) |
| Environment / support check | — | — | ✅ `nitroctl doctor` |

> Every ACPI/WMI/EC detail above was reverse engineered from the OEM Windows
> binaries and this laptop's own ACPI tables — see
> [`docs/REVERSE_ENGINEERING.md`](docs/REVERSE_ENGINEERING.md) and the raw
> evidence in [`re/`](re/).

---

## How it works (short version)

The OEM Windows tools contain **no ring-0 drivers** for these features. Both are
thin clients:

```
NitroSense.exe ──pipe──► PSSvc ──┐
                                 ├──► ACPI-WMI (\root\wmi) ──► Embedded Controller (EC)
Care Center  ──pipe──► ACCStd ───┘
```

Linux already speaks ACPI-WMI, so the logic ports directly:

* **Battery limiter / calibration / temperature** → GUID `79772EC5-…`,
  ACPI method `WMBE`. Handled by the bundled `acer-wmi-battery` module.
* **Keyboard RGB** → GUID `7A4DDFE7-…`, ACPI method `WMBH`. Handled by the
  bundled `acer-wmi-rgb` module, which writes zone colours into EC RAM.
* **Profile + fans + temps** → already implemented by the mainline `acer-wmi`
  driver (`platform-profile` and `hwmon`).

Full protocol write-up (WMI GUID map, method IDs, IPC framing, the OEM
`gmInput` bit layouts): [`docs/REVERSE_ENGINEERING.md`](docs/REVERSE_ENGINEERING.md).

---

## Compatibility

**Not limited to the AN515-58** — each feature maps to a WMI GUID, and it works
wherever your firmware exposes that GUID. Verified running only on AN515-58;
the rest is derived from the OEM app's own model list, the mainline driver's
quirk table, and the community battery-driver model list.

| Layer | Depends on | Breadth |
|---|---|---|
| **Battery limiter / calibration / temp** | WMI GUID `79772EC5-…` in firmware | Broad — confirmed on many Acer laptops (Nitro 5 AN515-57/58, AN517-54, Nitro V15 ANV15-51, Nitro 16/ANV16-42, Predator Helios Neo 16 PHN16-71, Triton 300 SE, plus Aspire/Swift/Enduro/TravelMate models) |
| **Performance profile + fans + temps** | mainline `acer-wmi` | AN515-58 has its own quirk; several Predators too. Other Nitro/Predator models usually work via the `predator_v4=1` module param (or `force_caps=<bitmask>`) |
| **4-zone keyboard RGB** | WMI GUID `7A4DDFE7-…` in firmware | The shipped NitroSense supports 28 Nitro/Predator models, all with 4-zone RGB (`LightingType=1`, `PerKey=0`) → interface is shared, but confirm per model |

### Check any Acer model

```bash
# do the firmwares expose the GUIDs we need? (run before loading anything)
ls /sys/bus/wmi/devices/ | grep -i 79772EC5   # -> battery limiter
ls /sys/bus/wmi/devices/ | grep -i 7A4DDFE7   # -> keyboard RGB

# after loading the modules, this is the authoritative check:
nitroctl doctor
```

`nitroctl doctor` reporting `[OK]` for a driver means the module bound, which in
turn means the GUID is present and the methods were reachable.

**If the performance profile / fans are missing on a model that isn't in the
mainline quirk table**, load the mainline driver with:

```bash
sudo modprobe -r acer-wmi
sudo modprobe acer-wmi predator_v4=1        # platform-profile + hwmon
```

If that is not enough, `force_caps=<value>` lets you set the capability bitmask
directly (see `modinfo acer-wmi`).

### Per-model differences (from the shipped NitroSense)

The OEM app ships 28 per-model config directories, grouped by `MachineType`:
`0` (older UI), `1`, and `2` (2022+ "Octavia" UI). Every one of them declares
4 lighting zones and CPU+GPU fans, so the RGB/fan commands are the same shape
across models — the AN515-58 simply happens to be `MachineType=2`, which is why
this port used the "Octavia" UI paths.

> Only AN515-58 was physically tested here. On another model, start with
> `nitroctl doctor` and the two `ls` checks above before assuming anything.

---

## Requirements

* An Acer Nitro / Predator laptop that exposes the WMI GUIDs above
  (verified on AN515-58). Run `nitroctl doctor` to check.
* A kernel with the `wmi` bus and the in-tree `acer-wmi` module
  (`CONFIG_ACER_WMI=m`, present in Arch/`linux-zen`).
* Kernel headers/build tree matching `uname -r` **only if you build the modules
  from source** (see the note below).
* Python 3.8+ for the CLI (no third-party packages).

### Kernel headers vs. running kernel

The `linux-*-headers` package must match `uname -r` **exactly** or the built
`.ko` will not load. On Arch the headers package often moves ahead of the
installed kernel (e.g. headers `7.2.9` while the running kernel is `7.2.6`).
`install.sh` and `make` both use `/lib/modules/$(uname -r)/build`, so you need
headers for the **running** kernel. If they are not in the repos, fetch them
from the Arch Linux Archive:

```bash
# figure out the exact running kernel, e.g. 7.2.6-zen2-1-zen
uname -r
# download the matching headers and extract just the build tree
curl -LO https://archive.archlinux.org/packages/l/linux-zen-headers/linux-zen-headers-<uname-r>-x86_64.pkg.tar.zst
tar --zstd -xf linux-zen-headers-*.pkg.tar.zst usr/lib/modules/<uname-r>/build
# build against the extracted tree
make -C kernel KDIR="$PWD/usr/lib/modules/<uname-r>/build"
```

Debian/Ubuntu: `sudo apt install linux-headers-$(uname -r)`.

---

## Installation

### A. Just try it (no install)

The three drivers are what provide the interfaces; load them first (once per
boot), then use the CLI straight from the source tree.

```bash
cd ~/Project/reverse/acer-linux

# load the modules (run once per boot)
sudo modprobe acer-wmi-battery acer-wmi-rgb
#   or, before installing anywhere:
#   sudo insmod kernel/acer-wmi-battery.ko && sudo insmod kernel/acer-wmi-rgb.ko

# sanity check
./cli/nitroctl doctor
./cli/nitroctl battery status
```

### B. Full install (recommended)

Installs the modules for the running kernel, puts `nitroctl` in
`/usr/local/bin` (so **`sudo nitroctl` works** — sudo's `secure_path` does not
include `~/.local/bin`), and configures boot-time loading + a udev rule.

```bash
cd ~/Project/reverse/acer-linux
sudo ./scripts/install.sh
```

After this:

```bash
sudo nitroctl battery limit on     # works
```

Uninstall:

```bash
sudo ./scripts/uninstall.sh
```

### C. Run the RGB without sudo

By default the sysfs attributes are root-owned, so `nitroctl rgb set` needs
root. The bundled udev rule (installed by option B) relaxes them to
group-writable. To install it on its own:

```bash
sudo install -m 0644 scripts/udev/60-acer-nitro-rgb.rules /etc/udev/rules.d/
sudo udevadm control --reload && sudo udevadm trigger --subsystem-match=wmi
```

Then a normal user can run `nitroctl rgb set ...`.

### D. Persist a colour / profile at login

Edit and enable the example unit, or call `nitroctl` from your window
manager's autostart:

```bash
sudo cp scripts/systemd/nitro-rgb.service /etc/systemd/system/
sudo systemctl enable --now nitro-rgb.service
```

For Hyprland, add to `~/.config/hypr/hyprland.conf`:
```
exec-once = nitroctl rgb set 8a2be2
exec-once = nitroctl perf set balanced
```

---

## Usage

```
nitroctl <command> [options]
```

Commands: `battery`, `rgb`, `perf`, `fan`, `doctor`.

### Battery (Care Center)

```bash
nitroctl battery status            # limit + calibration + temperature
nitroctl battery limit on          # stop charging at ~80%   [needs root]
nitroctl battery limit off         # charge to 100%         [needs root]
nitroctl battery calibrate on      # start calibration cycle [needs root]
nitroctl battery calibrate off     # stop calibration        [needs root]
nitroctl battery temp              # raw battery temperature (0.1 K units)
```

### Keyboard RGB (NitroSense)

```bash
nitroctl rgb get                       # current colours of all 4 zones
nitroctl rgb set 8a2be2                # whole keyboard           [needs root]
nitroctl rgb set ff0000 -z 1,2         # only zones 1 and 2       [needs root]
nitroctl rgb set 00ff00 -z 3           # only zone 3              [needs root]
nitroctl rgb off                       # all zones off            [needs root]
```

### Performance profile (NitroSense)

```bash
nitroctl perf                          # show current + available profiles
nitroctl perf list                     # same
nitroctl perf set quiet                # cool & quiet             [needs root]
nitroctl perf set balanced             # default                  [needs root]
nitroctl perf set performance          # max performance          [needs root]
```

Available choices on the reference machine:
`low-power quiet balanced balanced-performance performance`.

### Fans & temperatures (NitroSense)

```bash
nitroctl fan status                    # fan RPM, temperatures, PWM mode
nitroctl fan set 1 70                  # CPU fan -> 70% (manual)  [needs root]
nitroctl fan set 2 100                 # GPU fan -> 100%          [needs root]
nitroctl fan auto                      # return control to firmware [needs root]
```

### Environment check

```bash
nitroctl doctor
```

Example output:

```
nitroctl 1.0.0  |  host check
------------------------------------------------------------
[OK  ] machine                Nitro AN515-58
[OK  ] acer-wmi (in-tree)     platform-profile + fans + temps
[OK  ] acer-wmi-battery       charge limiter available
[OK  ] acer-wmi-rgb           keyboard RGB available
[OK  ] hwmon (fans/temps)     /sys/devices/platform/acer-wmi/hwmon/hwmon8
[OK  ] platform-profile       balanced
```

---

## Examples

```bash
# Desk work at a cafe, plugged in all day -> preserve battery life
sudo nitroctl battery limit on

# Gaming session -> max performance + red keyboard
sudo nitroctl perf set performance
sudo nitroctl rgb set ff0000

# Night time -> quiet fans, RGB off
sudo nitroctl perf set quiet
sudo nitroctl rgb off

# Watch temperatures while compiling / rendering
watch -n2 nitroctl fan status
```

---

## Verifying the RGB driver

The bundled script exercises the full read → write → read-back round trip
against the EC (safe to re-run; it is idempotent):

```bash
sudo ./scripts/verify-rgb.sh 00ff00     # set all zones green, then read back
sudo ./scripts/verify-rgb.sh ff0000     # restore the default
```

A `SUCCESS:` line means the EC stored the value; check the physical keyboard to
confirm the LEDs changed.

---

## Layout

```
acer-linux/
├── cli/nitroctl                     # the CLI (single dependency-free Python file)
├── kernel/
│   ├── acer-wmi-battery.c           # battery health/calibration/temp WMI driver
│   ├── acer-wmi-rgb.c               # 4-zone keyboard RGB WMI driver
│   └── Makefile
├── docs/REVERSE_ENGINEERING.md      # full protocol write-up
├── re/                              # evidence: ACPI tables, decompiled sources, tools
│   ├── acpi/ssdt14.dsl              # the ACPI table holding the Acer WMI methods
│   ├── acpi/WMI_GUID_MAP.txt        # decoded GUID -> method map
│   ├── decompiled/                  # key OEM .NET sources (IPC framing, bit packing)
│   └── tools/decode_wdg.py          # ACPI _WDG decoder
└── scripts/
    ├── install.sh  uninstall.sh
    ├── verify-rgb.sh
    ├── modules-load.d/  modprobe.d/  udev/  systemd/
```

---

## Troubleshooting

**`sudo: nitroctl: command not found`**
`sudo` uses its own `secure_path` that does not include `~/.local/bin`. Either
run `sudo ./scripts/install.sh` (installs to `/usr/local/bin`), or call the
full path: `sudo ~/Project/reverse/acer-linux/cli/nitroctl ...`.

**`ERR: acer-wmi-rgb driver not loaded` / `acer-wmi-battery driver not loaded`**
The module is not loaded. `sudo modprobe acer-wmi-battery acer-wmi-rgb`, or
`sudo insmod kernel/<module>.ko`. `nitroctl doctor` shows which are present.

**`insmod: ERROR: could not insert module ...: File exists`**
Not an error — the module is already loaded. Check with
`lsmod | grep acer_wmi`. Re-running `insmod` is unnecessary.

**The built module will not load (`invalid module format`)**
Headers/running-kernel mismatch. Use the prebuilt `.ko`, or build against
matching headers (see "Kernel headers vs. running kernel" above).

**RGB changes are not saved after reboot**
The EC resets zone colours on power cycle. Persist them with the systemd unit
or an `exec-once` line (see Installation, D).

**`Permission denied` writing to `/sys/...`**
Those attributes are root-only. Use `sudo`, or install the udev rule
(Installation, C).

**Fans stayed at a fixed speed after `fan set`**
`fan set` writes the hwmon PWM in *manual* mode, which overrides firmware
control. Restore it with `nitroctl fan auto`.

---

## Notes & caveats

* **The battery limiter is a 2-state toggle** — on ≈ 80 %, off = 100 %. There is
  no arbitrary percentage knob; that is a firmware limitation, not a bug. The
  ACPI battery trip point (`_BTP`) is *not* the charge limiter.
* **RGB is 4 zones, not per-key.** This panel reports `PerKey=0` in the OEM
  config; animated effects are firmware-side. The CLI sets static zone colours.
* **`fan set` overrides firmware fan control** and is clamped to ≥ 20 % to avoid
  overheating. Prefer `nitroctl perf set ...` for normal use and reserve manual
  PWM for special cases.
* Only a **single battery** is assumed (matching what the OEM software does).
* Everything here was verified on one machine (AN515-58). Other models that
  expose the same WMI GUIDs should work; `nitroctl doctor` tells you.

---

## Credits

* Battery WMI protocol cross-checked against the community driver
  [`frederik-h/acer-wmi-battery`](https://github.com/frederik-h/acer-wmi-battery)
  (same GUID and method IDs), on whose supported-model list the AN515-58 appears.
* Performance-profile / fan / temperature support comes from the mainline
  Linux `acer-wmi` driver.

## License

Kernel modules: **GPL-2.0-or-later** (required for kernel modules).
CLI and scripts: **MIT**.
