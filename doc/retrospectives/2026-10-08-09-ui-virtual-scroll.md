# 2026-10-08 - P5 Virtual ScrollRect

## Work and baseline

- Goal: fixed-height vertical single-column virtualization over an existing native ScrollRect.
- Branch: `codex/game-ui-p5-scroll`; source revision `8f8abd290d540dc0b6ba30c5acc8b36b271bc302`.
- Status: P5 implementation and focused automated validation complete; P6/P7 remain open.
- Contract: [Game UI System](../GAME_UI_SYSTEM.md), [human API](../api/UI.md), [AI API](../ai/api/UI.md).

## Decisions and changes

- `VirtualScrollRect` borrows the ScrollRect, viewport/content, prefab, and project data. It owns bounded row clones, generation tokens, staging storage, and its own listener. The supported first scope is fixed-height, single-column vertical layout with finite spacing, no horizontal padding, and no content/cell layout drivers.
- Extent is `count * height + max(0, count - 1) * spacing`; zero rows have zero extent. The viewport row budget is `ceil(viewportHeight / (height + spacing)) + 1 + 2 * overscan`; initial prefill creates at most `min(Count, budget)` cells. After Count shrinks, retained cells may remain up to the viewport budget, including at Count zero (CountActive is zero, while CountInactive/CountOwned may be nonzero). Viewport shrink trims excess retained clones.
- `Refresh()` explicitly replaces visible generations after data changes. Automatic scroll/viewport reconciliation preserves unchanged in-range bindings. Failure cleans partial work, reports an aggregate error, and stops automatic retry until an explicit command.
- Async project work must return to Unity's main thread and check both the captured token and `IsCurrent` before applying a result. Cached index/generation/token remain readable after cancellation; `View` and `IsCurrent` are main-thread-only.
- P5 changed development `Assets/TPLab/UI` source only. UI remains outside released 0.0.1 package contents/tag.

## Validation and limits

- Red: EditMode 7/7 failed and PlayMode 7/7 failed, skip0; failures came from `NotImplementedException` in signature stubs. [Red Edit](../validation/ui-system/p5/red-edit.json) | [Red Play](../validation/ui-system/p5/red-play.json).
- First Green on original Editor PID 24376: EditMode 31/31 and PlayMode 48/48, failed0/skip0. [First Green Edit](../validation/ui-system/p5/first-green-edit.json) | [First Green Play](../validation/ui-system/p5/first-green-play.json).
- Final focused UI79 passed EditMode 31/31 and PlayMode 48/48, failed0/skip0; product Console was empty. [Final Edit](../validation/ui-system/p5/final-edit.json) | [Final Play](../validation/ui-system/p5/final-play.json). Edit runId `7984bff4b98d43e0b8aba050ba85b45e`; Play connector run `34780-1791455587583642700` recovered the completed result after CLI timeout, with no rerun.
- Final input snapshot verified 406 entries: 405 non-protected source matches and six protected files unchanged. [Source match](../validation/ui-system/p5/source-commit-match.json).
- Fourteen P5 tests cover native ScrollRect extent/window/index-to-Text mapping, counts 0/1/100/1000/10000, scrollbar and synthetic drag/inertia, shrink/restore, resize, disable/destroy, partial bind failure, and delayed stale-generation completion. Synthetic PointerEventData is not physical-device or Input System UX evidence.
- Warmed fixed-viewport counter stability demonstrates bounded ownership/reuse only. P7 consumer install, graphics Player, native-versus-virtual 1000-row performance, separate 10000-row scalability, Profiler measurement, Canvas composition, broad UX, and user acceptance remain NotRun.

## Next work

- Continue with P6 installer/settings, then P7 consumer/Player/profiler/Canvas/user-acceptance gates. Do not describe functional counter stability as measured performance.
