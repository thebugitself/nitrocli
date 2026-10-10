#!/usr/bin/env python3
"""Decode Acer ACPI _WDG buffers (from iasl .dsl output) into a GUID -> method map.

Usage:  acpidump -b ; iasl -d dsdt.dat ssdt*.dat
        ./decode_wdg.py *.dsl
"""
import re, struct, sys

def parse(path):
    txt = open(path, encoding="utf-8", errors="replace").read()
    for m in re.finditer(r"Name \(_WDG, Buffer \(0x([0-9A-Fa-f]+)\)\s*\{(.*?)\}\)", txt, re.S):
        size = int(m.group(1), 16)
        bs = bytes(int(x, 16) for x in re.findall(r"0x([0-9A-Fa-f]{2})", m.group(2)))
        yield size, bs[:size]

def guid(b):
    d1 = struct.unpack("<I", b[0:4])[0]
    d2 = struct.unpack("<H", b[4:6])[0]
    d3 = struct.unpack("<H", b[6:8])[0]
    return f"{d1:08X}-{d2:04X}-{d3:04X}-{b[8]:02X}{b[9]:02X}-" + "".join(f"{x:02X}" for x in b[10:16])

for path in sys.argv[1:]:
    for size, b in parse(path):
        print(f"== {path} _WDG {len(b)} bytes ==")
        for off in range(0, len(b) - 19, 20):
            e = b[off:off + 20]
            objid, inst, flags = e[16:18], e[18], e[19]
            kind = "method" if flags & 0x02 else ("event" if flags & 0x08 else "?")
            label = objid.decode() if objid[0] and all(0x20 <= c < 0x7f for c in objid) else f"0x{struct.unpack('<H', objid)[0]:04X}"
            print(f"  {{{guid(e[:16])}}}  inst={inst} flags=0x{flags:02X}  {kind} object={label}")
