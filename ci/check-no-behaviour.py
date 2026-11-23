#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
# SPDX-License-Identifier: GPL-3.0-or-later
"""The app holds no behaviour (ADR-078 D-13).

windows-app assembles Core, the display shell and the hardware shell; only
its wiring reads a command or builds an event. This fails when any other
source file under Vauchi/ reads a command's name, names the contract's
types, matches a command by its name, or builds an event variant as JSON
(a dictionary, an anonymous object or a JSON writer), as linux-app's
lint:no-behaviour does for Command:: and Event:: outside src/wiring.rs.

The wiring: MainWindowEffects.cs (the commands no shell runs before the
hardware shell reports), Hardware/ (hands Core's hardware commands to
windows-hw and its events back) and the two platform observations
linux-app also reports from its wiring, Platform/NetworkCost.cs and
Platform/DeepLinks.cs.

    check-no-behaviour.py [APP_SOURCE_DIRECTORY]
"""

import re
import sys
from pathlib import Path

WIRING = (
    "MainWindowEffects.cs",
    "Hardware/",
    "Platform/NetworkCost.cs",
    "Platform/DeepLinks.cs",
)

# A variant name is PascalCase with at least two words (BleStartScanning,
# QrScanned); the contract has no one-word command or event.
VARIANT = r"[A-Z][a-z0-9]+[A-Z][A-Za-z0-9]*"
BEHAVIOUR = re.compile(
    r"CommandName\("  # reading a command
    r"|\bVauchi\.Contract\b|\bContractJson\b"  # the contract's types
    rf'|case\s+"{VARIANT}"\s*:|"{VARIANT}"\s*=>'  # a command matched by its name
    rf'|\["{VARIANT}"\]\s*=\s*new\b'  # an event built as a JSON dictionary
    rf"|\bnew\s*\{{\s*{VARIANT}\s*=\s*new\b"  # ... as an anonymous object
    rf'|WriteStartObject\("{VARIANT}"\)'  # ... by a JSON writer
)
LINE_COMMENT = re.compile(r"^\s*//")


def is_wiring(relative):
    return any(relative == entry or (entry.endswith("/") and relative.startswith(entry)) for entry in WIRING)


def violations(directory):
    sources = sorted(
        path for path in directory.rglob("*.cs")
        if not {"bin", "obj"} & set(path.relative_to(directory).parts)
    )
    if not sources:
        # A renamed or empty directory must not read as a clean one.
        yield f"{directory}: no source file to check"
        return
    for source in sources:
        relative = source.relative_to(directory).as_posix()
        if is_wiring(relative):
            continue
        for number, line in enumerate(source.read_text(encoding="utf-8-sig").splitlines(), start=1):
            if LINE_COMMENT.match(line):
                continue
            found = BEHAVIOUR.search(line)
            if found:
                yield f"{relative}:{number}: {found.group(0).strip()}"


def main(arguments):
    directory = Path(arguments[0]) if arguments else Path(__file__).resolve().parents[1] / "Vauchi"
    found = list(violations(directory))
    for violation in found:
        print(violation, file=sys.stderr)
    if found:
        print(
            f"{len(found)} place(s) outside the wiring read a command or build an event (ADR-078 D-13)",
            file=sys.stderr,
        )
        return 1
    print(f"{directory}: only the wiring reads a command or builds an event")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
