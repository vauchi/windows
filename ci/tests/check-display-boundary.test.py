#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
# SPDX-License-Identifier: GPL-3.0-or-later
"""The display-boundary check passes on a clean folder and fails on each planted use."""

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CHECK = ROOT / "ci/check-display-boundary.py"
CLEAN = "using System;\nusing Microsoft.UI.Xaml;\n\nnamespace Vauchi.CoreUI;\n\npublic sealed class Host\n{\n}\n"


class DisplayBoundaryCheckTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.folder = Path(self.temporary.name) / "CoreUI"
        self.folder.mkdir()
        self.source = self.folder / "Host.cs"
        self.source.write_text(CLEAN, encoding="utf-8")

    def tearDown(self):
        self.temporary.cleanup()

    def check(self, directory=None):
        arguments = [sys.executable, str(CHECK)]
        if directory is not False:
            arguments.append(str(directory or self.folder))
        return subprocess.run(arguments, capture_output=True, text=True, check=False, timeout=60)

    def test_the_repositorys_display_shell_names_no_core_binding(self):
        result = self.check(False)

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("names no Core binding and no app service", result.stdout)

    def test_a_clean_folder_passes(self):
        self.assertEqual(0, self.check().returncode)

    def test_each_forbidden_name_is_refused_with_its_place(self):
        for line, named in (
            ("using Vauchi.Interop;", "Vauchi.Interop"),
            ("using Vauchi.Services;", "Vauchi.Services"),
            ("using Vauchi.Handlers;", "Vauchi.Handlers"),
            ("var e = VauchiNative.AppDispatch(handle, json);", "VauchiNative"),
            ("string close = Localizer.T(\"action.close\");", "Localizer"),
            ("[LibraryImport(\"vauchi_cabi\")]", "LibraryImport"),
            ("[DllImport(\"kernel32\")]", "DllImport"),
            ("NetworkCost.Watch(send, queue);", "NetworkCost"),
            ("var key = KeyStorageService.RetrieveKey();", "KeyStorageService"),
            ("_exchange = new ExchangeCommandHandler(send, queue, window);", "ExchangeCommandHandler"),
        ):
            with self.subTest(line=line):
                self.source.write_text(CLEAN + line + "\n", encoding="utf-8")

                result = self.check()

                self.assertEqual(1, result.returncode, result.stdout)
                self.assertIn(f"Host.cs:9: names {named}\n", result.stderr)

    def test_markup_is_read_too(self):
        (self.folder / "Host.xaml").write_text(
            '<UserControl xmlns:app="using:Vauchi.Services" />\n', encoding="utf-8"
        )

        result = self.check()

        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("Host.xaml:1: names Vauchi.Services", result.stderr)

    def test_a_comment_that_mentions_a_name_passes(self):
        self.source.write_text(
            CLEAN + "    // The app implements this over VauchiNative; nothing here calls it.\n",
            encoding="utf-8",
        )

        self.assertEqual(0, self.check().returncode)

    def test_names_that_only_resemble_a_forbidden_one_pass(self):
        self.source.write_text(
            CLEAN + "var x = new LocalizerLike(); var y = MyNetworkCostView; var z = AppLocalizer;\n",
            encoding="utf-8",
        )

        self.assertEqual(0, self.check().returncode)

    def test_build_output_is_not_read(self):
        generated = self.folder / "obj"
        generated.mkdir()
        (generated / "Host.g.cs").write_text("using Vauchi.Interop;\n", encoding="utf-8")

        self.assertEqual(0, self.check().returncode)

    def test_an_empty_or_missing_folder_is_not_a_clean_one(self):
        empty = Path(self.temporary.name) / "empty"
        empty.mkdir()

        for directory in (empty, Path(self.temporary.name) / "renamed-away"):
            with self.subTest(directory=directory.name):
                result = self.check(directory)

                self.assertEqual(1, result.returncode, result.stdout)
                self.assertIn("no source file to check", result.stderr)


if __name__ == "__main__":
    unittest.main()
