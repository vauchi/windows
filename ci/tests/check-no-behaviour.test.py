#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
# SPDX-License-Identifier: GPL-3.0-or-later
"""check-no-behaviour.py fails on each kind of behaviour outside the wiring (CC-27)."""

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CHECK = ROOT / "ci/check-no-behaviour.py"
CLEAN = "namespace Vauchi;\n\npublic sealed class Window\n{\n    void Save() { settings.Values[\"WindowWidth\"] = 3; }\n}\n"


class NoBehaviourCheckTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.app = Path(self.temporary.name) / "Vauchi"
        (self.app / "Hardware").mkdir(parents=True)
        (self.app / "Platform").mkdir()
        (self.app / "Window.cs").write_text(CLEAN, encoding="utf-8")

    def tearDown(self):
        self.temporary.cleanup()

    def check(self, directory=None):
        arguments = [sys.executable, str(CHECK)]
        if directory is not False:
            arguments.append(str(directory or self.app))
        return subprocess.run(arguments, capture_output=True, text=True, check=False, timeout=60)

    def test_the_repositorys_app_keeps_its_behaviour_in_the_wiring(self):
        result = self.check(False)

        self.assertEqual(0, result.returncode, result.stderr)

    def test_a_clean_app_passes(self):
        self.assertEqual(0, self.check().returncode)

    def test_each_kind_of_behaviour_outside_the_wiring_is_refused_with_its_place(self):
        cases = {
            "reads a command": "switch (PresentationState.CommandName(command)) { }",
            "names the contract": "using Vauchi.Contract;",
            "decodes with the contract": "var c = ContractJson.DecodeCommand(bytes);",
            "builds an event": 'var e = new Dictionary<string, object> { ["ImageReceived"] = new() };',
            "matches a command in a switch arm": '"BleStartScanning" => Kind.Scan,',
            "matches a command in a case": 'case "SetScreenBrightness":',
            "builds an event as an anonymous object": "var e = JsonSerializer.Serialize(new { QrScanned = new { data } });",
            "builds an event with a JSON writer": 'w.WriteStartObject("BleDeviceDiscovered");',
        }
        for name, line in cases.items():
            with self.subTest(name):
                source = self.app / "Handler.cs"
                source.write_text(f"class Handler\n{{\n    {line}\n}}\n", encoding="utf-8")

                result = self.check()

                self.assertEqual(1, result.returncode, name)
                self.assertIn("Handler.cs:3:", result.stderr)
                source.unlink()

    def test_the_wiring_may_read_commands_and_build_events(self):
        (self.app / "MainWindowEffects.cs").write_text(
            "class E { void F() { switch (PresentationState.CommandName(c)) { } } }\n", encoding="utf-8")
        (self.app / "Hardware" / "Bridge.cs").write_text("using Vauchi.Contract;\n", encoding="utf-8")
        (self.app / "Platform" / "NetworkCost.cs").write_text(
            'var e = new Dictionary<string, object> { ["NetworkCostChanged"] = new() };\n', encoding="utf-8")

        self.assertEqual(0, self.check().returncode)

    def test_a_file_named_like_the_wiring_elsewhere_is_not_wiring(self):
        (self.app / "Platform" / "MainWindowEffects.cs").write_text(
            "class E { void F() { var n = PresentationState.CommandName(c); } }\n", encoding="utf-8")

        self.assertEqual(1, self.check().returncode)

    def test_a_comment_that_names_a_command_passes(self):
        (self.app / "Notes.cs").write_text("// PresentationState.CommandName( is the wiring's\n", encoding="utf-8")

        self.assertEqual(0, self.check().returncode)

    def test_build_output_is_not_read(self):
        (self.app / "obj").mkdir()
        (self.app / "obj" / "Generated.cs").write_text("using Vauchi.Contract;\n", encoding="utf-8")

        self.assertEqual(0, self.check().returncode)

    def test_an_empty_or_missing_folder_is_not_a_clean_one(self):
        empty = Path(self.temporary.name) / "empty"
        empty.mkdir()
        for directory in (empty, Path(self.temporary.name) / "renamed-away"):
            with self.subTest(directory.name):
                self.assertEqual(1, self.check(directory).returncode)


if __name__ == "__main__":
    unittest.main()
