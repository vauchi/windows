#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
# SPDX-License-Identifier: GPL-3.0-or-later
"""The display shell renders against the contract and links no Core.

`Vauchi/CoreUI` is the WinUI display shell. ADR-078 makes it a library
the app assembles: it reaches the engine through an interface the app
implements (`IEngineLink`) and names neither Core's C ABI nor an app
service. This fails when a source file there does, which is what keeps
the folder movable into a library project (vauchi/private#1234).

    check-display-boundary.py [DIRECTORY]
"""

import re
import sys
from pathlib import Path

# Core's C ABI, the app's services and handlers, and any P/Invoke.
FORBIDDEN = re.compile(
    r"\b(?:VauchiNative|Vauchi\.Interop|Vauchi\.Services|Vauchi\.Handlers|Vauchi\.Helpers"
    r"|Localizer|KeyStorageService|SecureStorageService|ExchangeCommandHandler"
    r"|BleExchangeService|DirectSendService|NetworkCost|SystemTrayManager"
    r"|LibraryImport|DllImport|vauchi_cabi)\b"
)
LINE_COMMENT = re.compile(r"^\s*//")


def violations(directory):
    sources = sorted(
        path for path in directory.rglob("*")
        if path.suffix in (".cs", ".xaml") and not {"bin", "obj"} & set(path.parts)
    )
    if not sources:
        # A renamed or empty directory must not read as a clean one.
        yield f"{directory}: no source file to check"
        return
    for source in sources:
        for number, line in enumerate(source.read_text(encoding="utf-8-sig").splitlines(), start=1):
            if LINE_COMMENT.match(line):
                continue
            found = FORBIDDEN.search(line)
            if found:
                yield f"{source}:{number}: names {found.group(0)}"


def main(arguments):
    directory = Path(arguments[0]) if arguments else Path(__file__).resolve().parents[1] / "Vauchi/CoreUI"
    found = list(violations(directory))
    for violation in found:
        print(violation, file=sys.stderr)
    if found:
        print(f"{len(found)} violation(s): the display shell must reach the engine through IEngineLink only", file=sys.stderr)
        return 1
    print(f"{directory}: names no Core binding and no app service")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
