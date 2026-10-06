"""Recheck saved evidence and its exact local inputs; this does not run Unity tests."""
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[3]
EVIDENCE = Path(__file__).resolve().parent


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


for mode, count in (("EditMode", 176), ("PlayMode", 96)):
    result = read_json(EVIDENCE / ("full-" + mode + ".json"))
    assert (result["total"], result["passed"], result["failed"], result["skipped"]) == (count, count, 0, 0), mode
    assert len(result["passes"]) == count and not result["failures"]

native = read_json(EVIDENCE / "native-editor.json")
assert all(native[key] for key in ("BuildRejected", "PlayRejected", "LiveInvalidRootDetected", "FixtureRemoved"))
assert not native["Error"] and "Bootstrap validation failed" in native["BuildDiagnostic"]
assert read_json(EVIDENCE / "final-console.json") == []
assert "Unity: ready" in (EVIDENCE / "final-status.txt").read_text(encoding="utf-8-sig")

for name in ("test-inputs.json", "preserved-user-inputs.json"):
    for path, expected in read_json(EVIDENCE / name).items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest().lower() == expected.lower(), path

all_guids = {}
for meta in (ROOT / "Assets").rglob("*.meta"):
    match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8-sig"), re.M)
    if match:
        all_guids.setdefault(match.group(1), []).append(meta)
owned = list((ROOT / "Assets/MyLab/Core/SceneManagement").glob("*.cs"))
owned += list((ROOT / "Assets/MyLab/Editor/Bootstrap").glob("*.cs"))
owned += [ROOT / path for path in (
    "Assets/MyLab/Tests/EditMode/BootstrapSystemTests.cs",
    "Assets/MyLab/Tests/PlayMode/BootstrapPlayModeTests.cs",
    "Assets/MyLab/Tests/Fixtures/BootstrapCallbacksProbe.cs",
    "Assets/MyLab/Tests/Fixtures/BootstrapHub.unity",
    "Assets/MyLab/Validation/Editor/BootstrapEditorCheck.cs")]
for asset in owned:
    meta = Path(str(asset) + ".meta")
    guid = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8-sig"), re.M).group(1)
    assert len(all_guids[guid]) == 1, meta

for path in (
    "README.md", "doc/BOOTSTRAP_SYSTEM.md", "doc/INDEX.md", "doc/CORE_PLAN.md",
    "doc/GAME_SCENE_MANAGER_DRAFT.md", "doc/retrospectives/INDEX.md",
    "doc/retrospectives/2026-10-06-12-bootstrap-system.md", "doc/validation/bootstrap-system/README.md"):
    doc = ROOT / path
    for target in re.findall(r"\[[^\]]+\]\(([^)]+)\)", doc.read_text(encoding="utf-8-sig")):
        if "://" not in target and not target.startswith("#"):
            assert (doc.parent / target.split("#")[0]).resolve().exists(), (path, target)
assert not (ROOT / "Assets/MyLabBootstrapValidationProbe.unity").exists()
assert not list((ROOT / "Assets").glob("BootstrapTest_*"))
print("PASS: test counts, native gates, exact inputs, user preservation, owned GUIDs, links and fixture cleanup")
