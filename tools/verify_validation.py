"""Verify saved Unity evidence against its local source inputs, without running Unity tests.

Usage: python tools/verify_validation.py --evidence doc/validation/FEATURE
The evidence folder supplies checks.json, test-inputs.json and preserved-inputs.json.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    folder = args.evidence.resolve()
    if not folder.is_relative_to(root / "doc/validation"):
        parser.error("Evidence must be inside this checkout's doc/validation")

    def read(name):
        return json.loads((folder / name).read_text(encoding="utf-8-sig"))

    def local(path):
        target = (root / path).resolve()
        if not target.is_relative_to(root):
            raise AssertionError(f"Input escapes checkout: {path}")
        return target

    checks = read("checks.json")
    for name, counts in checks["results"].items():
        result = read(name)
        actual = [result[key] for key in ("total", "passed", "failed", "skipped")]
        assert actual == counts and counts[0] > 0, name
        assert len(result["passes"]) == counts[1] and len(result["failures"]) == counts[2], name
    for path, expected in read("test-inputs.json")["sha256"].items():
        data = local(path).read_bytes()
        if not path.endswith(".dll"):
            data = data.replace(b"\r\n", b"\n")
        assert hashlib.sha256(data).hexdigest().lower() == expected.lower(), path
    for path, expected in read("preserved-inputs.json").items():
        assert hashlib.sha256(local(path).read_bytes()).hexdigest().lower() == expected.lower(), path
    for path in checks["documents"]:
        doc = local(path)
        for target in re.findall(r"\[[^\]]+\]\(([^)]+)\)", doc.read_text(encoding="utf-8-sig")):
            if "://" not in target and not target.startswith("#"):
                assert (doc.parent / target.split("#")[0]).resolve().exists(), (path, target)
    guids = {}
    for meta in (root / "Assets").rglob("*.meta"):
        match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8-sig"), re.M)
        if match:
            guids.setdefault(match.group(1), []).append(meta)
    for path in checks["assets"]:
        asset = local(path)
        assert asset.exists(), path
        meta = Path(str(asset) + ".meta")
        guid = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8-sig"), re.M).group(1)
        assert len(guids[guid]) == 1, path
    assert read("final-console.json") == []
    assert "Unity: ready" in (folder / "final-status.txt").read_text(encoding="utf-8-sig")
    for pattern in checks.get("absentAssetPatterns", []):
        assert not list((root / "Assets").glob(pattern)), pattern
    print("PASS: saved counts, current source hashes, protected bytes, GUIDs, document links and final Editor evidence")


if __name__ == "__main__":
    main()
