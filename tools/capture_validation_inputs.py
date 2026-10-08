"""Capture current checkout inputs for saved Unity validation; does not run tests or approve integration."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


def digest(data, text):
    return hashlib.sha256(data.replace(b"\r\n", b"\n") if text else data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", type=Path, required=True)
    parser.add_argument("--base", required=True)
    parser.add_argument("--preserved", type=Path, required=True)
    parser.add_argument("--additional-input", action="append", default=[], help="Additional literal file inside the selected checkout used by this test fixture.")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    folder = args.evidence.resolve()
    if not folder.is_relative_to(root / "doc/validation") or not folder.is_dir():
        parser.error("Select an existing evidence directory inside this checkout's doc/validation")
    preserved = json.loads(args.preserved.read_text(encoding="utf-8-sig"))
    for path, expected in preserved.items():
        target = (root / path).resolve()
        assert target.is_relative_to(root) and digest(target.read_bytes(), False).lower() == expected.lower(), path
    files = subprocess.check_output([
        "git", "ls-files", "-z", "--cached", "--others", "--exclude-standard", "--",
        "Assets/TPLab", "Assets/Plugins", "Packages", "ProjectSettings/ProjectVersion.txt", "tools"
    ], cwd=root).decode("utf-8").split("\0")
    for value in args.additional_input:
        target = (root / value).resolve()
        if not target.is_relative_to(root) or not target.is_file():
            parser.error("Additional inputs must be existing files inside this checkout")
        files.append(target.relative_to(root).as_posix())
    hashes = {}
    for path in sorted(set(files)):
        if path and Path(path).suffix in {".cs", ".asmdef", ".unity", ".asset", ".meta", ".dll", ".json", ".inputactions", ".txt", ".py"}:
            hashes[path] = digest((root / path).read_bytes(), not path.endswith(".dll"))
    (folder / "test-inputs.json").write_text(json.dumps({
        "baseCommit": args.base, "normalization": "Text CRLF to LF; DLL bytes unchanged.", "sha256": hashes
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (folder / "preserved-inputs.json").write_text(json.dumps(preserved, indent=2) + "\n", encoding="utf-8")
    print(f"Captured {len(hashes)} inputs; {len(preserved)} protected files unchanged.")


if __name__ == "__main__":
    import sys
    if sys.argv[1:] == ["--self-check"]:
        assert digest(b"line\r\n", True) == digest(b"line\n", True)
        assert digest(b"\x00\r\n", False) != digest(b"\x00\n", False)
        print("PASS: text normalization and raw binary hashing")
    else:
        main()
