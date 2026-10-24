"""Keep dependency proposals coupled to both independently qualified lock files."""

import ast
import json
import re
import shlex
import textwrap
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PAIRS = {
    "requirements-ci.txt": "requirements-ci.lock",
    "tests/scope_card/browser-requirements.txt": "tests/scope_card/browser-requirements.lock",
}


class LockPairingTests(unittest.TestCase):
    def test_lock_manager_covers_both_pairs_without_duplicate_proposals(self) -> None:
        config = json.loads((ROOT / "renovate.json").read_text())
        self.assertFalse(config["pip_requirements"]["enabled"])
        patterns = config["pip-compile"]["managerFilePatterns"]
        for lock in PAIRS.values():
            with self.subTest(lock=lock):
                self.assertEqual(
                    sum(bool(re.fullmatch(pattern[1:-1], lock)) for pattern in patterns), 1
                )
        self.assertFalse(config["automerge"])
        self.assertTrue(config["lockFileMaintenance"]["enabled"])

    def test_headers_expose_only_the_actual_source_with_explicit_flag_values(self) -> None:
        for source, lock in PAIRS.items():
            with self.subTest(lock=lock):
                header = (ROOT / lock).read_text().splitlines()[1]
                command = shlex.split(header.removeprefix("#    "))
                self.assertEqual(command[:3], ["uv", "pip", "compile"])
                self.assertIn("--python-version=3.15.1", command)
                self.assertIn("--output-file=" + lock, command)
                self.assertIn("--universal", command)
                self.assertIn("--generate-hashes", command)
                self.assertEqual([arg for arg in command[3:] if not arg.startswith("-")], [source])

    def test_actual_workflow_generator_preserves_discoverable_flag_syntax(self) -> None:
        workflow = (ROOT / ".github/workflows/lock-refresh.yml").read_text()
        block = workflow.split("python - <<'PYCODE'", 1)[1].split("PYCODE", 1)[0]
        tree = ast.parse(textwrap.dedent(block))
        assignments = [
            node
            for node in ast.walk(tree)
            if isinstance(node, ast.Assign)
            and any(isinstance(target, ast.Name) and target.id == "args" for target in node.targets)
        ]
        self.assertEqual(len(assignments), 1)
        value = assignments[0].value
        if not isinstance(value, ast.List):
            self.fail("The compiler invocation requires explicit arguments.")
        constants = [item.value for item in value.elts if isinstance(item, ast.Constant)]
        self.assertIn("--python-version=3.15.1", constants)
        self.assertIn("--universal", constants)
        self.assertNotIn("--python-version", constants)
        self.assertNotIn("-o", constants)
        outputs = [item for item in value.elts if isinstance(item, ast.JoinedStr)]
        self.assertEqual(len(outputs), 1)
        self.assertEqual(ast.unparse(outputs[0]), "f'--output-file={lock}'")
