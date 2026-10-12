# 2026-10-12 · UI sample frame clearing

Original Editor PID24376 / Unity6000.3.18f1. Source base `bb4372bb17ef9e471e28ff27f835a1ccb4e1581e`; only the sample Builder and two Bootstrap scenes changed. Exact hashes and preserved inputs are in [summary](summary.json). Runtime/public API unchanged; final physical/visual acceptance remains pending.

The [observed Game view](overlap-before.png) contains old Status values, closed popups and cover text. [Live object state](overlap-state.txt) shows one current Status and no live popups. There were no scene Cameras. A temporary common-root `SolidColor`, opaque, geometry-free Camera removed the stale pixels in the same active Play state: [corrected same-state view](overlap-camera-after.png). This was a rendered-frame defect, rather than accumulated Text objects or a managed-depth error.

[Executed owned repair](repair.cs.txt), [saved result](repair.txt): the existing two sample Bootstrap scenes now include a common-root child background Camera; the Builder generates the same configuration. No existing asset was regenerated and no prefab/meta/public contract changed. Single persists that child with the existing common root; Additive retains it in Bootstrap.

Fresh original-Editor command-path verification:

- Single: first loading/Continue, A/B/C, middle B and C close, automatic Hub→Main replacement. [Closed view](single-close.png), [final view](single-final.png), [observed Ready/one persistent Camera](single-final.txt).
- Additive: same sequence. [Closed view](additive-close.png), [final view](additive-final.png), [observed Ready/one Bootstrap Camera](additive-final.txt).
- In both final views, old status values, closed B/C and cover/loading text are gone. [Final Console](console-final.json) is empty; [compilation/current configuration](compile-final.txt) is idle with UI source inputs13/6/7 and both formerly omitted presentation test files retained.

Buttons were invoked through their actual project onClick events; no physical input or new unit-suite pass is claimed. Fresh unit tests0, Player builds0, performance runs0. The prior P7 Players predate this sample camera correction. User's initial two Console exceptions concerned invalid command order/current-HUD reselection; Console was cleared before these fresh flows. Play entry was confirmed from actual native state; Additive start used `editor play --wait`.

Five original user files and all three changed assets' metas remain unchanged. UI review build settings were temporarily Single then returned to the five enabled Additive scenes; their raw hash differs from the diagnostic baseline and is reported, not staged. The original build-settings/Init setup backup remains in the active manual review folder. Runtime runInBackground was temporarily enabled for unattended frames then restored tofalse; the corrected Additive demo stays playing with manual continuation restored on. Main merge and branch deletion remain pending user acceptance.
