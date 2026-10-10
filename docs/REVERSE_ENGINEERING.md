# Reverse engineering notes — Acer Care Center + NitroSense → Linux

Target machine: **Acer Nitro 5 AN515-58** (`product_name = Nitro AN515-58`,
`product_version = V2.19`, board `Jimny_ADH`, DMI vendor `Acer`).

Analysed artefacts:

| Artefact | What it is | Why |
|---|---|---|
| `Acer Care Center_v4.00.3060 .../CareCenter_x64.msi` | MSI installer | contains `ACCStd.exe`, `AccNvmeDST`, `ADSPPlugin/BatteryDevice.dll`, ... |
| `.../CareCenterUWP/x64/*.appx` | UWP app | Care Center UI + `ACCDiagnosticPlg.dll` (battery element) |
| `NitroSense_V3.01.3056 .../NitroSense.msi` | MSI installer | contains the `PSSvc` service (`FILE_APP_SVC_EXE`), agents, profile helper |
| `.../NitroSenseV3.1/*.appxbundle` | UWP app | `NitroSense.exe` (the UI) + `TsDotNetLib.dll` (IPC protocol) |
| `.../Plugs/Nitro AN515-58/*.ini` | per-model config | model type, lighting zones, GPU OC table |

There are **no ring-0 drivers** (`.sys`) in either package: everything is done
in user space through **ACPI-WMI** control methods (`root\wmi`) and the embedded
controller (EC) that sits behind them. That is exactly why the logic ports to
Linux cleanly — Linux already speaks ACPI-WMI.

---

## 1. Tooling used

```
7z x  (appxbundle -> appx -> content, and .msi payload extraction)
ilspycmd  (dotnet tool)          # .NET decompilation -> C#
file / strings -e l             # wide (UTF-16) strings on native PE images
acpidump + iasl -d              # dump & disassemble ACPI DSDT/SSDT
git clone frederik-h/acer-wmi-battery   # cross-check of the battery WMI protocol
```

`ilspycmd` was installed with `dotnet tool install -g ilspycmd`; the OEM
assemblies are `.NET` but the UI ones need the Windows WPF projections, some
tables only decompile partially (see `il/`).

---

## 2. NitroSense: the IPC layer (client side)

`NitroSense.exe` is only a **UI client**. All hardware access goes through a
named pipe to the `PSSvc` Windows service:

```
pipe name : \\.\pipe\PredatorSense_service_namedpipe
```

The wire format is built by `TsDotNetLib.IPCMethods.SendCommandByNamedPipe`:

```
offset 0  : uint16  cmdCode          # see ServiceCommand enum
offset 2  : uint8   argCount
offset 3  : for each arg:
              int32 sizeOnMemory
              bytes payload          # strings are UTF-16LE + NUL
```

Command codes (from `TsDotNetLib/ServiceCommand.cs`, reconstructed to the
exact enum values because the enum is indexed from 0):

| cmd | ServiceCommand | meaning |
|----:|----------------|---------|
| 9  | kSvcCmdGamingPorfileWMISetFunction  | `SetAcerGamingFunction` set |
| 10 | kSvcCmdGamingPorfileWMIGetFunction  | `AcerGamingFunction` get |
| 11 | kSvcCmdGamingLEDGroupColorSetFunction | 4-zone "hotkey" group colour |
| 12 | kSvcCmdGamingLEDGroupColorGetFunction | read group colour |
| 13 | kSvcCmdGetGamingSysinfoFunction | temperatures / fan RPM |
| 14 | kSvcCmdGetGPUUsageLoading | GPU load |
| 15 | kSvcCmdGamingFanGroupBehaviorSetFunction | fan Auto/Max/Custom |
| 16 | kSvcCmdGamingFanGroupSpeedSetFunction | fan % |
| 17 | kSvcCmdWMISetFunction | generic set |
| 18 | kSvcCmdWMISetLogoLED | lid/logo LED |
| 19 | kSvcCmdWMISetLogoBehavior | lid/logo behaviour |
| 20 | kSvcCmdWMIGetFunction | generic get |
| 21 | kSvcCmdNvidiaOCGetFunction | |
| 22 | kSvcCmdIntelOCGetFunction | |
| 23 | kSvcCmdNvidiaOCSetFunction | |
| 24 | kSvcCmdIntelOCSetFunction | |
| 27 | kSvcCmdWMISetGamingKBBacklight | brightness |
| 28 | kSvcCmdWMISetGamingRgbKbSetting | per-zone RGB |
| 29 | kSvcCmdWMISetGamingLEDBehavior | zone enable bits |
| 30 | kSvcCmdSetOperationMode | Quiet / Default / Performance |
| 31 | kSvcCmdGPUGetCount | |
| 32 | kSvcCmdGPUGetFrequency | |
| 33 | kSvcCmdWMISetGamingMISCSetting | misc (e.g. dGPU switch) |
| 34 | kSvcCmdWMIGetGamingMISCSetting | misc read |

Response framing: 5 bytes of header, then the payload. e.g. a `uint32` result
is read from `resp[5..9]`, a `uint64` from `resp[5..13]`.

### The real interface: Acer's WMI

`PSCreateDefaultProfile.exe` contains the canonical call, which names both the
namespace and the class:

```csharp
ManagementScope scope = new ManagementScope(@"\root\wmi");
ObjectQuery q = new ObjectQuery("SELECT * FROM AcerGamingFunction");
// GetMethodParameters(<name>);  in["gmInput"] = <u64>;  out = invoke(...)["gmOutput"]
```

So on Windows the chain is:

```
NitroSense.exe --pipe--> PSSvc --\root\wmi "AcerGamingFunction"--> ACPI-WMI --> EC
```

The `gmInput` is a **packed 64-bit value**; the class method name is one of
`SetGamingProfile`, `GetGamingProfile`, `SetGamingLED`, `SetGamingLEDGroupColor`,
`SetGamingRgbKb`, `SetGamingKBBacklight`, `SetGamingFanBehavior`,
`SetGamingFanSpeed`, `SetGamingFanGroupBehavior`, `SetGamingFanGroupSpeed`,
`SetGamingFanGroupSpeed`, `SetGamingMiscSetting`, `GetGamingSystemInformation`,
… (strings + `WMIFunction.cs` in the decompiled tree).

---

## 3. NitroSense: the packed `gmInput` bit layouts

These come straight from `CommonFunction.cs` / `Popup_Lighting.xaml.cs` /
`Nitro_MainWindow_Octavia.xaml.cs` in the decompiled `NitroSense.exe`.

### 3.1 Keyboard RGB — per zone (`SetGamingRgbKb`, pipe cmd 28)

```
u64 = zone | (R << 8) | (G << 16) | (B << 24)
zone bit  : 1 = zone1, 2 = zone2, 4 = zone3, 8 = zone4
R/G/B     : 0..255, already pre-scaled by zone_kb_adjust = {0.8, 1.0, 1.0}
```

Per-zone brightness (`Change_Zone_Brightness_RGB`) is applied by scaling R/G/B
(0% / 50% / 100%).

### 3.2 Zone enable mask (`SetGamingLEDBehavior`, pipe cmd 29)

```
u64 = 8
    | (zone1_on ? 1<<40 : 0)
    | (zone2_on ? 1<<41 : 0)
    | (zone3_on ? 1<<42 : 0)
    | (zone4_on ? 1<<43 : 0)
```

### 3.3 Keyboard brightness (`SetGamingKBBacklight`, pipe cmd 27)

```
u64 = (brightness - 1) * 25 << 16      # brightness 1..4 -> 0/25/50/75
```

### 3.4 Hotkey group colour (`SetGamingLEDGroupColor`, pipe cmd 11)

```
u64 = (group_index & 0xFF) | (R<<8) | (G<<16) | (B<<24)
R = ceil(color.R * 0.5)                # note the 0.5 factor
```
`group_index` 0/1/2 → group head names `r` / `b` / `g` (WASD / arrows / "hot keys").

### 3.5 Lid/logo LED (`SetLogoLED`, pipe cmd 18)

```
u64 = index | (R<<8) | (G<<16) | (B<<24)
index = 0x01 (left) / 0x02 (right) / 0x03 (…) / 0x05 / 0x06
```
`WMISetLogoStatus` (cmd 19) toggles it.

### 3.6 Fan control

`SetGamingFanGroupBehavior` (cmd 15):

```
auto  : u64 = 9 | (0x41 << 16)
max   : u64 = 9 | (0x82 << 16)
custom: u64 = 9 | (0xC3 << 16)
custom, explicit per-group:
    CPU auto? 1 : 3   -> bits 16..21   (num = 9 | (cpuAuto<<16) | (gpuAuto<<22))
    GPU auto? 1 : 3   -> bits 22..27
single CPU: 1 | (auto?1:3)<<16
single GPU: 8 | (auto?1:3)<<22
```
`SetGamingFanGroupSpeed` (cmd 16):

```
u64 = group | (percentage << 8)        # group 1 = CPU, 4 = GPU
```

CoolBoost (`WMISetFunction` cmd 17): `u64 = 7 | (state ? 1 : 0) << 16`;
read with `WMIGetFunction(519)` → bit0-7 status, bit8-15 state.

### 3.7 Temperatures & fan RPM (`GetGamingSystemInformation`, cmd 13)

```
input  = 1 | (index << 8)
output: status = out & 0xFF ; value = (out >> 8) & 0xFFFF
index (System_Health_Information_Index):
  1 CPU_Temperature   2 CPU_Fan_Speed    3 System_Temperature  4 System_Fan_Speed
  5 FrostCore         6 GPU_Fan_Speed    7 System2_Temperature 8 System2_Fan_Speed
  9 GPU2_Fan_Speed   10 GPU1_Temperature 11 GPU2_Temperature
```

### 3.8 Advanced settings (`SetGamingPorfileConfiguration`, cmd 9 / 10)

```
set group index : ((group_index << 32) | 4)
win/menu key    : (((status ? 1 : 0) << 24) | 2)
LCD overdrive   : (0x10 | (state ? 1<<48 : 0))
read  (cmd 10, input 0):  bit0-7 status; bit32-39 group; bit48-55 lcd overdrive
```

### 3.9 Misc (`SetGamingMiscSetting`, cmd 33 — e.g. dGPU)

```
wm_set(2 | ((!mode ? 1 : 2) << 8))        # 1 = discrete, 2 = hybrid
wm_get(9) >> 8 & 0xFF == 3               # "switchable graphics supported"
```

### 3.10 Performance profile (`SetOperationMode`, pipe cmd 30)

`Operation_Mode` enum:

```
0 Quiet   1 Default   2 Render   3 Edit   4 Extreme   5 Turbo
```

On the AN515-58 (2022 / "Octavia" UI, `_feature_type == 2`) only Quiet(0),
Default(1) and Performance(4) are exposed by `Nitro_MonitoringPage_2022.xaml.cs`.
Selecting mode 0 also sends `gpu_set_whispermodestatus(1)`.

---

## 4. Care Center: the battery limiter

### 4.1 Client → service

`ACCDiagnosticPlg/BatteryElementCtl.cs` sends a `TransferData` struct to a
named pipe (`PipeDef._ACCPipeName`) with these commands (`Commands` enum,
values recovered as *counts from the enum start*) to `ACCStd.exe`:

| cmd | call |
|----:|------|
| 35 | probe support |
| 36 | `GetBatteryHealthControlStatus` |
| 39 | `SetOpenBatteryHealthmode`  (charge limit ON) |
| 40 | `SetCloseBatteryHealthmode` (charge limit OFF) |
| 41 | `SetCloseBatteryCalibrationmode` |
| 42 | `SetOpenBatteryCalibrationmode` |
| 45 | register event |
| 48 | get calibration status |
| 50 | get battery calibration |

### 4.2 Service → WMI

`ACCStd.exe` (`Acer.CareCenter.ACCStd.MainWindow`) does:

```csharp
scope = new ManagementScope(@"\root\wmi");
// battery health: class method "GetBatteryHealthControlStatus" / "SetBatteryHealthControl"
// generic BIOS   : "SELECT * FROM APGeAction", method "SetFunction"/"GetFunction"
//                  with properties uiInput (u64) / uiOutput (u64)
// generic OEM    : "SELECT * FROM AcerGenericMethod", method "SetFunction"
```

### 4.3 The real WMI GUID / method IDs

The OEM power/battery WMI interface is **not** called "AcerGamingFunction" for
battery — the method lives on the *Acer Application Base Driver* GUID. Dumping
and disassembling the machine's own ACPI tables (`acpidump`/`iasl`) shows the
mapping in the `_WDG` buffer of `SSDT14` (OEM ID `ACRSYS`):

```
GUID 79772EC5-04B1-4BFD-843C-61E7F77B6CC9   instance 1   flags 0x02  -> method "BE"
GUID 79772EC6-04B1-4BFD-843C-61E7F77B6CC9   instance 1   flags 0x02  -> method "BF"
GUID F75F5666-B8B3-4A5D-A91C-7488F62E5637   instance 1   flags 0x02  -> method "BK"  (keyboard)
GUID FE1DBBDA-3014-4856-870C-5B3A744BF341   instance 1   flags 0x02  -> method "BL"
GUID 77B0C3A7-F71D-43CB-B749-91CBFF5DDC43   instance 1   flags 0x02  -> method "BG"
GUID 7A4DDFE7-5B5D-40B4-8595-4408E0CC7F56   instance 1   flags 0x02  -> method "BH"  (keyboard)
GUID 4BB53443-488A-430D-A25C-874660E23BDD   instance 1   flags 0x02  -> method "AC"
GUID 9F98130E-3B48-40B3-9402-DE8E160F30CC   instance 1   flags 0x02  -> method "AB"
GUID 1EA0BC57-521E-461B-A7A9-5D36F141A82A   instance 1   flags 0x02  -> method "AE"
GUID 61EF69EA-865C-4BC3-A502-A0DEBA0CB531   instance 1   flags 0x02  -> method "AA"
```

Each ASL method is a thin dispatcher:

```asl
Method (WMBE, 3, Serialized) {          // WMBE == GUID 79772EC5
    CTID = 0x4542
    WSMI (Arg1, Arg2)                   // WSMI stores MTID=Arg1, WMIB=Arg2, WSSP=0xD0 (SMI)
    Switch (ToInteger (Arg1)) {
        Case (0x13) { BEBI = WMIB; Return (BEBI) }   // get battery info index
        Case (0x14) { BEGB = WMIB; Return (BEGB) }
        Case (0x15) { BESB = WMIB; Return (BESB) }   // set battery
    }
}
```

The ACPI method IDs for the battery are therefore **0x13 / 0x14 / 0x15** on
GUID `79772EC5-…`, exactly matching the argument order seen in the OEM
software: `wmi_evaluate_method(GUID, 0, <methodid>, …)`.

Cross-checked against the Linux community driver
`frederik-h/acer-wmi-battery`, which uses the very same GUID and the same
method ids (this is also what picks method id **19 / 20 / 21 = 0x13/0x14/0x15**
for, respectively, "get battery information", "get health/calibration status"
and "set health/calibration status"):

```c
#define WMI_GUID "79772EC5-04B1-4bfd-843C-61E7F77B6CC9"
wmi_evaluate_method(WMI_GUID, 0, 19, ...)   /* get_battery_information(index, battery) */
wmi_evaluate_method(WMI_GUID, 0, 20, ...)   /* get_battery_health_control_status     */
wmi_evaluate_method(WMI_GUID, 0, 21, ...)   /* set_battery_health_control(fn,state)  */
```

**AN515-58 is on that project's confirmed model list** (MODELS.md, issue #57).
The charge limiter is a 2-state "health mode":

```
health mode ON  -> firmware stops charging at ~80%
health mode OFF -> firmware charges to 100%
function mask: 1 = HEALTH_MODE, 2 = CALIBRATION_MODE
```

Data structures (from the driver, byte-exact):

```
get_battery_health_control_status input :  { uBatteryNo=1, uFunctionQuery=1, rsv[2] }   (4 B)
get_battery_health_control_status output:  { uFunctionList, uReturn[2], uFunctionStatus[5] } (8 B)
set_battery_health_control input        :  { uBatteryNo=1, uFunctionMask, uFunctionStatus, rsv[5] } (8 B)
set_battery_health_control output       :  { uReturn, rsv }  (2..4 B)
```

Battery temperature is `get_battery_information(0x8, 1)` (Smart Battery Data
Spec index 0x08 = temperature), returned in 0.1 K → °C = value/10 − 273.15.

---

## 5. The absolutely critical part: this machine already has the ACPI objects

`acpidump`/`iasl` on the live machine produced `dsdt.dsl` + `ssdt1..22.dsl`.
The relevant objects are:

* `\_SB.WMID` (PNP0C14, `_UID = "APGe"`) in `SSDT14`, carrying the full `_WDG`
  above and the methods `WMAA`, `WMAB`, `WMAE`, `WMBE`, `WMBK`, `WMBL`, `WMBG`,
  `WMBH`, `WMAC`.
* `\_SB.PC00.LPCB.EC0_` — the EC, whose fields are written by `WMBH`
  (keyboard) and read/written by the battery methods.
* `\_SB.PC00.LPCB.BAT1` (`PNP0C0A`) — the standard control-method battery with
  `_BIF`/`_BST`/`_BTP`. Note `_BTP` (battery trip point) is **present but only
  writes `EC0.BTPB`** — it is *not* the 80% charge limiter, which is why there is
  no generic Linux knob for it.

Because the kernel `wmi` bus enumerates every `_WDG` entry as
`/sys/bus/wmi/devices/<GUID>-<inst>`, any out-of-tree WMI driver can bind to the
battery GUID and the keyboard GUID without touching the firmware.

### 5.1 The keyboard RGB path (method `WMBH` / GUID `7A4DDFE7`)

`WMBH` implements the per-zone static RGB write/read that NitroSense uses:

```
Arg1 == 6  (set):
    in  = { zone, r, g, b, logoFlag, logoIndex, 0, 0, 0, 0 }
    zone: 1=zone1 2=zone2 4=zone3 8=zone4
            -> EC0.KB1R/KB1G/KB1B .. KB4R/KB4G/KB4B
    group hotkey colours -> EC0.LB1R/G/B .. LB2
    out = { status, 0, 0, 0 }                     // status 0 == OK

Arg1 == 7  (get):
    in  = { zone, 0, ... }
    out = { status, r, g, b }                      // reads EC0.KB*R/G/B back
```

The 10-byte input buffer, the zone selection bits and the `{status,r,g,b}`
return are all taken verbatim from the `WMBH` ASL body; `zone_sel = {1,2,4,8}`
matches both the ASL and the `zone_kb_adjust`/`Button_tag` logic in
`Popup_Lighting.xaml.cs`.

---

## 6. What was actually verified on this machine

```
$ ./nitroctl doctor
[OK]   machine                Nitro AN515-58
[OK]   acer-wmi (in-tree)     platform-profile + fans + temps
[OK]   acer-wmi-battery       charge limiter available
[OK]   hwmon (fans/temps)     /sys/devices/platform/acer-wmi/hwmon/hwmon8
[OK]   platform-profile       balanced

$ ./nitroctl battery status
charge limit (health mode) : disabled (charge to 100%)   [0]
calibration mode           : inactive   [0]
battery temperature        : 34.9 C

$ ./nitroctl fan status
fan1 (CPU) : 3658 RPM        fan2 (GPU) : 4054 RPM
temp1 (CPU) : 87 C   temp2 (GPU?) : 65 C   temp3 (SYS/Chipset) : 68 C
pwm1 mode : auto   pwm2 mode : auto

$ cat /sys/devices/platform/acer-wmi/platform-profile/platform-profile-0/choices
low-power quiet balanced balanced-performance performance
```

The `acer-wmi-battery` module (built against the running kernel's headers)
loads, binds `79772EC5-…-4`, and its WMI calls succeed (`temperature` returns a
plausible value, `health_mode`/`calibration_mode` read back from the firmware).

The `acer-wmi-rgb` module binds `7A4DDFE7-…-8` and the full set/get round trip
was exercised against the EC:

```
$ nitroctl rgb get                     # initial state == HW_Support.ini default
zone1: #ff0000   zone2: #ff0000   zone3: #ff0000   zone4: #ff0000

$ echo 00ff00 > .../acer-wmi-rgb/zone1..4     # write
zone1: #00ff00   zone2: #00ff00   zone3: #00ff00   zone4: #00ff00   # read back

# writes persist in the EC and are visible from the host afterwards:
zone1: #00ff00   zone2: #00ff00   zone3: #00ff00   zone4: #00ff00
```

Battery limiter round trip and performance profile were exercised the same way:

```
health_mode : 0 -> set 1 -> 1 -> set 0 -> 0        (firmware re-read, not cached)
temperature : 34900 (0.1 K)  == 34.9 C
profile     : balanced -> performance -> quiet -> balanced
```

### 6.1 Feature → Linux mechanism summary

| Feature (Windows app) | Linux mechanism | Status |
|---|---|---|
| Care Center **Battery Charge Limit** (80%) | `acer-wmi-battery` WMI driver → `/sys/bus/wmi/drivers/acer-wmi-battery/health_mode` | ✅ works |
| Care Center **Battery Calibration** | same driver `calibration_mode` | ✅ works |
| Care Center **Battery Temperature** | same driver `temperature` | ✅ 34.9 °C read |
| NitroSense **Performance profile** | mainline `acer-wmi` → `platform-profile` | ✅ works |
| NitroSense **Fan RPM / temperatures** | mainline `acer-wmi` → `hwmon` | ✅ works |
| NitroSense **Fan Auto/Max/Custom** | mainline `acer-wmi` → `hwmon` `pwm1/2` | ⚠ manual PWM only |
| NitroSense **4-zone Keyboard RGB** | `acer-wmi-rgb` (this repo) → `zone1..4` | ✅ verified (write+read via EC) |
