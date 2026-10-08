# P5 Virtual ScrollRect validation

SourceRevision: `8f8abd290d540dc0b6ba30c5acc8b36b271bc302`.
ValidationStatus: automated functional gate passed; P7 consumer, graphics Player, performance and user acceptance pending.

Same original TPLab Editor PID24376, Unity6000.3.18f1, Connector0.4.1. Sources were frozen during each actual test run. No replacement Editor, zero-total result, or automatic test rerun substitutes for these results.

| Actual run | EditMode | PlayMode | Failed | Skipped |
|---|---:|---:|---:|---:|
| New P5 Red | 7 | 7 | 14 expected stub failures | 0 |
| First whole UI Green | 31 | 48 | 0 | 0 |
| Final committed-source whole UI Green | 31 | 48 | 0 | 0 |

Actual Red files: [Edit](red-edit.json), [Play](red-play.json). All 14 failed at the unimplemented Configure stub; this establishes an actual Red gate, not 14 independently reached failure paths. [First Edit](first-green-edit.json), [first Play](first-green-play.json), [final Edit](final-edit.json), [final Play](final-play.json) retain individual test identities. Final Edit run is `7984bff4b98d43e0b8aba050ba85b45e`. Final Play used the same completed connector run `34780-1791455587583642700` after CLI return1; no second test run was started. The normalized result verifies nonzero counts and original Editor identity.

The seven new Edit and seven Play methods exercise counts0/1/100/1,000/10,000, actual visible Graphic/Text index-position coverage, native scrollbar/drag/inertia, count and viewport shrinking, fixed-viewport warmed creation/retirement stability, disable/enable/destruction, delayed canceled-generation results, invalid settings/reentry and partial bind failure cleanup. Warmed object counts are functional bounds, not measured CPU/GC improvement or a comparison to a native 1,000-cell list. Synthetic EventSystem drag does not establish physical-device or final visual acceptance.

[Final input snapshot](test-inputs.json) has406 entries. [Commit match](source-commit-match.json) verifies405 nonprotected inputs against the source commit; the dirty protected SampleInput entry is explicitly excluded from Git comparison. [Protected raw hashes](preserved-inputs.json) preserve all6 user-owned changes. [Source whitespace correction](source-whitespace-fix.json) removed only extra EOF blank lines before the source commit; test bodies were unchanged, and final actual tests follow that commit. [Checks](checks.json) cover current hashes, document links and unique GUIDs. [Final status](final-status.txt) is ready and [product Console](final-console.json) has0 errors.

## Original Editor registration limitation

AssetDatabase GUID/importers remain present, but after domain reload the original Editor script registry again omits UIPresentation and its two presentation test files. [Final observation](final-registration-observation.json) shows no active tests, compilation/update/Play or dirty active scene, loaded UI types, and the three omissions. The guarded one-time [first](first-green-compile-registration.json) and [final](final-compile-registration.json) compiler registration unions retained all7,336 existing paths and added exactly3 owned paths. [Compiler response paths](compiler-input-paths.json) contain those three and all four P5 scripts for the actual compiler inputs. This is not a permanent native registry repair. P7 must independently compile a fresh source consumer; it cannot replace original-Editor regression evidence.

The Red import/registration preflight and [final Edit prestart](final-edit-prestart.json) discovery failure started0 tests and remain separate from actual runs. A failed read-only diagnostic used an inaccessible internal type; the corrected observation uses reflection. None of those diagnostic attempts is counted as a Red/Green test run.

P6 settings/root/sample integration is next. P7 must run fresh original regression, base/optional-input consumer builds, graphics Player checks, same1,000 native/virtual measurements, separate10,000 scalability and Canvas composition comparisons. Main merge and branch deletion still require final user visual/real-input acceptance. UI is outside the released0.0.1 artifacts/tag; no version or Release was changed.
