"""Refuse invalid package inputs before emitting any release archive."""
import shutil
import json
import subprocess
import tempfile
import unittest
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class NativePackageRefusals(unittest.TestCase):
    def invoke(self, output, source=None):
        source = source or subprocess.check_output(
            ["git", "rev-parse", "HEAD"], cwd=ROOT, text=True
        ).strip()
        return subprocess.run(
            ["pwsh", "-NoProfile", "-File", "scripts/New-NativeReleasePackage.ps1",
             "-SourceCommit", source, "-OutputDirectory", str(output.relative_to(ROOT))],
            cwd=ROOT, text=True, capture_output=True,
        )

    def setUp(self):
        (ROOT / ".local").mkdir(exist_ok=True)
        self.output = Path(tempfile.mkdtemp(prefix="native-package-test-", dir=ROOT / ".local"))

    def tearDown(self):
        shutil.rmtree(self.output)

    def test_source_identity_must_match_checkout(self):
        result = self.invoke(self.output, "0" * 40)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("source must equal", result.stderr)
        self.assertEqual(list(self.output.iterdir()), [])

    def test_existing_output_survives_retry(self):
        archive = self.output / "careops-decisions-native.zip"
        archive.write_bytes(b"retained prior candidate")
        result = self.invoke(self.output)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Existing package output", result.stderr)
        self.assertEqual(archive.read_bytes(), b"retained prior candidate")

    def test_consumer_rejects_other_source_before_running_tests(self):
        archive = self.output / "other-source.zip"
        with zipfile.ZipFile(archive, "w") as payload:
            payload.writestr("native-release.json", json.dumps({"sourceCommit": "1" * 40}))
        result = subprocess.run(
            ["pwsh", "-NoProfile", "-File", "scripts/Test-NativeReleaseConsumer.ps1",
             "-SourceCommit", "0" * 40, "-Archive", str(archive),
             "-ConsumerDirectory", str(self.output / "consumer"),
             "-ResultsDirectory", str(self.output / "results")],
            cwd=ROOT, text=True, capture_output=True,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("package source identity mismatch", result.stderr)
        self.assertFalse((self.output / "results").exists())

    def test_mz_header_is_not_a_compiled_clr_library(self):
        dll = ROOT / "powerplatform/native/Authority/bin/Release/net462/CareOps.Decisions.Authority.dll"
        if dll.exists():
            self.skipTest("A real local build exists; never replace its assembly for a negative fixture")
        dll.parent.mkdir(parents=True, exist_ok=True)
        try:
            dll.write_bytes(b"MZ" + b"\x00" * 4094)
            result = self.invoke(self.output)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("GetAssemblyName", result.stderr)
            self.assertEqual(list(self.output.iterdir()), [])
        finally:
            dll.unlink()


if __name__ == "__main__":
    unittest.main()
