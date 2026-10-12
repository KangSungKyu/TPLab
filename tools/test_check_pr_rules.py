"""Tests for check_pr_rules.py using throwaway git repositories (no Unity, no network)."""
import contextlib
import io
import subprocess
import tempfile
import unittest
from pathlib import Path

import check_pr_rules as rules

TEST_FILE = "Assets/TPLab/Core/Tests/EditMode/SampleTests.cs"
BASE_TEST = """using NUnit.Framework;
public class SampleTests {
    [Test]
    public void A() { Assert.AreEqual(1, 1); Assert.IsTrue(true); }
    [Test, Timeout(1000)]
    public void B() { Assert.IsNull(null); }
}
"""


class RepoCase(unittest.TestCase):
    def setUp(self):
        self._dir = tempfile.TemporaryDirectory()
        self.addCleanup(self._dir.cleanup)
        self.repo = Path(self._dir.name)
        self.git("init", "-q", "-b", "main")
        self.write(TEST_FILE, BASE_TEST)
        self.write(TEST_FILE + ".meta", "meta")
        self.write("Assets/TPLab/Core/Pool.cs", "class Pool {}\n")
        self.write("Assets/TPLab/Core/Pool.cs.meta", "meta")
        self.base = self.commit("base")

    def git(self, *args):
        return subprocess.run(["git", "-c", "user.name=t", "-c", "user.email=t@t", *args], cwd=self.repo,
                              check=True, capture_output=True, text=True).stdout.strip()

    def write(self, relative, text):
        path = self.repo / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8", newline="\n")

    def commit(self, message):
        self.git("add", "-A")
        self.git("commit", "-q", "-m", message)
        return self.git("rev-parse", "HEAD")

    def run_rules(self):
        head = self.commit("change")
        errors, warnings, _ = rules.analyze(str(self.repo), self.base, head)
        return errors, warnings


class RuleTests(RepoCase):
    def test_clean_change_has_no_findings(self):
        self.write("Assets/TPLab/Core/Pool.cs", "class Pool { int x; }\n")
        self.assertEqual(self.run_rules()[:2], ([], []))

    def test_added_ignore_is_error(self):
        self.write(TEST_FILE, BASE_TEST.replace("    [Test]\n", "    [Test, Ignore(\"later\")]\n", 1))
        errors, _ = self.run_rules()
        self.assertTrue(any("끄는 구문" in message for _, message in errors))

    def test_removed_test_is_error_and_fewer_asserts_is_warning(self):
        self.write(TEST_FILE, BASE_TEST.split("    [Test, Timeout")[0] + "}\n")
        errors, warnings = self.run_rules()
        self.assertTrue(any("테스트 수 순감소" in message for _, message in errors))
        self.assertTrue(any("단언 수 순감소" in message for _, message in warnings))

    def test_moving_a_test_between_files_is_not_a_decrease(self):
        self.write(TEST_FILE, BASE_TEST.split("    [Test, Timeout")[0] + "}\n")
        self.write("Assets/TPLab/Core/Tests/EditMode/MovedTests.cs",
                   "public class M {\n    [Test, Timeout(1000)]\n    public void B() { Assert.IsNull(null); }\n}\n")
        self.write("Assets/TPLab/Core/Tests/EditMode/MovedTests.cs.meta", "meta")
        errors, _ = self.run_rules()
        self.assertEqual(errors, [])

    def test_sleep_in_test_is_warning_only(self):
        self.write(TEST_FILE, BASE_TEST.replace("Assert.IsNull(null);", "Thread.Sleep(75); Assert.IsNull(null);"))
        errors, warnings = self.run_rules()
        self.assertEqual(errors, [])
        self.assertTrue(any("시간 대기" in message for _, message in warnings))

    def test_new_cs_without_meta_is_error(self):
        self.write("Assets/TPLab/Core/New.cs", "class New {}\n")
        errors, _ = self.run_rules()
        self.assertEqual([path for path, _ in errors], ["Assets/TPLab/Core/New.cs"])

    def test_new_cs_with_meta_passes_and_tilde_folder_is_skipped(self):
        self.write("Assets/TPLab/Core/New.cs", "class New {}\n")
        self.write("Assets/TPLab/Core/New.cs.meta", "meta")
        self.write("Assets/TPLab/Samples~/Demo.cs", "class Demo {}\n")
        self.assertEqual(self.run_rules()[0], [])

    def test_deleted_cs_leaving_meta_is_error(self):
        (self.repo / "Assets/TPLab/Core/Pool.cs").unlink()
        errors, _ = self.run_rules()
        self.assertTrue(any("남아 있음" in message for _, message in errors))

    def test_root_without_meta_files_is_not_enforced(self):
        self.write("upm/package.json", "{}\n")
        self.write("upm/Runtime/Thing.cs", "class Thing {}\n")
        self.base = self.commit("upm without metas")
        self.write("upm/Runtime/Other.cs", "class Other {}\n")
        self.assertEqual(self.run_rules()[0], [])

    def test_protected_file_change_is_warning(self):
        self.write(".github/workflows/x.yml", "name: x\n")
        self.write("CLAUDE.md", "x\n")
        _, warnings = self.run_rules()
        self.assertEqual(sorted(path for path, _ in warnings), [".github/workflows/x.yml", "CLAUDE.md"])

    def test_linq_using_in_runtime_code_is_warning(self):
        self.write("Assets/TPLab/Core/Pool.cs", "using System.Linq;\nclass Pool {}\n")
        _, warnings = self.run_rules()
        self.assertTrue(any("System.Linq" in message for _, message in warnings))

    def test_content_that_looks_like_a_diff_header_is_kept_as_content(self):
        patch = "diff --git x.cs x.cs\n--- x.cs\n+++ x.cs\n@@ -1 +1 @@\n-old\n+++ new\n"
        parsed = rules.parse_patch(patch)
        self.assertEqual(parsed["x.cs"], {"added": ["++ new"], "removed": ["old"]})

    def test_rename_leaving_old_meta_is_error(self):
        self.git("mv", "Assets/TPLab/Core/Pool.cs", "Assets/TPLab/Core/Renamed.cs")
        self.write("Assets/TPLab/Core/Renamed.cs.meta", "meta")
        errors, _ = self.run_rules()
        self.assertEqual([path for path, _ in errors], ["Assets/TPLab/Core/Pool.cs"])
        self.assertIn("옛 .meta", errors[0][1])

    def test_rename_with_meta_moved_together_passes(self):
        self.git("mv", "Assets/TPLab/Core/Pool.cs", "Assets/TPLab/Core/Renamed.cs")
        self.git("mv", "Assets/TPLab/Core/Pool.cs.meta", "Assets/TPLab/Core/Renamed.cs.meta")
        errors, warnings = self.run_rules()
        self.assertEqual((errors, warnings), ([], []))

    def test_pure_rename_is_counted_as_a_changed_file(self):
        self.git("mv", "Assets/TPLab/Core/Pool.cs", "Assets/TPLab/Core/Renamed.cs")
        self.git("mv", "Assets/TPLab/Core/Pool.cs.meta", "Assets/TPLab/Core/Renamed.cs.meta")
        head = self.commit("rename")
        _, _, changed = rules.analyze(str(self.repo), self.base, head)
        self.assertGreaterEqual(changed, 2)

    def test_all_output_kinds_are_encodable_for_a_cp949_console(self):
        self.write(TEST_FILE, BASE_TEST.split("    [Test, Timeout")[0].replace("[Test]", "[Test, Ignore]", 1)
                   + "    public void C() { Thread.Sleep(5); }\n}\n")
        self.write("Assets/TPLab/Core/New.cs", "class New {}\n")
        self.write("Assets/TPLab/Core/Pool.cs", "using System.Linq;\nclass Pool {}\n")
        self.write("CLAUDE.md", "x\n")
        head = self.commit("all kinds")
        buffer = io.StringIO()
        with contextlib.redirect_stdout(buffer):
            rules.main(["--repo", str(self.repo), "--base", self.base, "--head", head])
        output = buffer.getvalue()
        for expected in ("끄는 구문", "시간 대기", ".meta 파일이 없음", "System.Linq", "규칙 파일이 변경됨", "테스트 수 순감소"):
            self.assertIn(expected, output)
        output.encode("cp949")

    def test_main_exit_codes(self):
        self.write(TEST_FILE, BASE_TEST.replace("    [Test]\n", "    [Test, Ignore]\n", 1))
        head = self.commit("ignore")
        argv = ["--repo", str(self.repo), "--base", self.base, "--head", head]
        self.assertEqual(rules.main(argv), 1)
        self.assertEqual(rules.main(argv + ["--warn-only"]), 0)


if __name__ == "__main__":
    unittest.main()
