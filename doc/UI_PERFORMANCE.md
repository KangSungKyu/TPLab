# P7 exploratory performance observations

**Scope:** one isolated Windows Mono graphics consumer run, not a product guarantee or a universal ranking. The run validates the copied source against `ee5e233744ac8ac4285a9d1340c974675b233400`, built one Windows64 Player, passed one startup smoke, and recorded scroll and Canvas scenarios. Run ID: `281f223279404092b6dc7714fd2980c6`. The optional Input-inclusive invocation also recorded successful regular smoke and six measured arms; its first separate sample then failed a fixture expectation. This table uses only the independent completed base run. The preserved [optional results](validation/ui-system/p7/consumer-input-additive-a/consumer-report.json) distinguish successful stages from that failed overall invocation.

Evidence: [consumer report](validation/ui-system/p7/consumer-base-c/consumer-report.json), [startup smoke](validation/ui-system/p7/consumer-base-c/smoke-validated.json), [raw scroll benchmark](validation/ui-system/p7/consumer-base-c/scroll-benchmark.json) and [validated scroll arms](validation/ui-system/p7/consumer-base-c/scroll-validated.json), [raw Canvas benchmark](validation/ui-system/p7/consumer-base-c/canvas-benchmark.json) and [validated Canvas arms](validation/ui-system/p7/consumer-base-c/canvas-validated.json). Raw evidence and the checked-in [runner](../tools/ui-consumer/README.md) retain the exact source, hardware, package and harness identities.

## Fixture and protocol

Unity 6000.3.18f1, Windows64 Mono2x Player, NVIDIA GeForce RTX 4060 Laptop GPU, Direct3D12, Windows 11 build 26200, 13th Gen Intel Core i7-13620H, 32 GiB RAM. The measured display was 1280x720; targetFrameRate=-1 and vSyncCount=0. This describes only the captured device/backend.

Scroll comparison used the same vertical ScrollRect fixture at viewport 1280x720, cell height 20, spacing 0, overscan 2, 120 warm-up frames, then 600 measured frames. Arms ran sequentially in fixed order **Native1000 → Virtual1000 → Virtual10000**; there was one sample per arm, no reversed/randomized order, and no repeated machine/session. The N=1000 pair is the like-for-like comparison. N=10000 is a separate scalability observation, not a baseline comparison.

Canvas comparison used the same HUD/Popup content: 128 HUD Graphics and up to three visible Popup displays of 25 Graphics each. The cycle was 120 frames: open P0/P1/P2 at frames 0/20/40, close P2/P1/P0 at 80/90/100; update dynamic HUD each frame, static HUD each 60 frames, visible popup text/color each 15 frames. Each arm had 120 warm-up and 600 measured frames, with zero schedule mismatch. Arms were SharedCanvas (1 Canvas), FrequencySeparatedCanvases (3), PopupPerCanvas (4). Each arm has one measured interval containing five 120-frame schedule cycles, after one warm-up cycle.

## Scroll results

All time values below are milliseconds, presented as median / P95 / maximum over 600 fresh measured frames. GC allocation is bytes per frame; memory is bytes; draw/batch/vertex counters are counts. These are raw recorder summaries, not rounded frame-rate estimates.

| Arm | Main Thread ms | Layout ms | Canvas.BuildBatch ms | UGUI UpdateBatches ms | GC Alloc/frame B | Total Used Memory B | GC Used Memory B | Draw Calls / Batches | Vertices |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Native1000 | 2.06075 / 2.4291 / 516.4046 | 0.0010 / 0.0015 / 0.0142 | 0.0121 / 0.0209 / 0.1885 | 1.7417 / 2.0228 / 13.8690 | 112 / 112 / 112 | 187090714 / 187119394 / 187131975 | 12734464 / 12763136 / 12767232 | 3 / 3 / 3 | 892 / 892 / 892 |
| Virtual1000 | 0.8080 / 1.4922 / 517.0482 | 0.0014 / 0.0027 / 0.0295 | 0.0075 / 0.0133 / 0.0647 | 0.39825 / 0.7065 / 2.8669 | 8560 / 9088 / 9088 | 172959790 / 173200587 / 173241817 | 10403840 / 10641408 / 10678272 | 3 / 3 / 3 | 892 / 892 / 892 |
| Virtual10000 | 1.3879 / 1.8870 / 512.8749 | 0.0018 / 0.0027 / 0.0192 | 0.0085 / 0.0118 / 0.0865 | 0.73235 / 0.8981 / 8.0860 | 25460 / 25460 / 25460 | 165022458 / 165116467 / 165161697 | 2473984 / 2568192 / 2617344 | 3 / 3 / 3 | 892 / 892 / 892 |

Ownership during the measured scroll interval: Native1000 kept 1000 active/owned rows (cumulative created 2500, destroyed 1500). Virtual1000 held 38–41 active, 0–3 inactive, 41 owned; cumulative created 41 and destroyed 0. Virtual10000 held the same 38–41 active, 0–3 inactive, 41 owned; cumulative created 41 and destroyed 0. At the recorded geometry endpoint, virtual lists had 38 unique bound indices, zero duplicates, zero missing visible rows, and viewport demand ceiling 42. The native arm bound the 1000 rows. This establishes bounded cell ownership and endpoint coverage for this fixture, not every input path or device.

The N=1000 virtual arm recorded lower median Main Thread and UGUI UpdateBatches values than the native arm in this run, but its GC allocation recorder was higher; both had the same observed draw calls/batches/vertices. Maximum Main Thread samples were large in all three arms. Treat these as measurements from one ordered exploratory run, not a speedup guarantee or proof of causality.

## Canvas results

Each statistic is median / P95 / max for 600 fresh frames. Draw calls and batches are the observed frame-count statistics; all arms had the same 128 endpoint HUD Graphics, 600 measured frames, and zero schedule mismatch.

| Arm | Canvases | Main Thread ms | Layout ms | Canvas.BuildBatch ms | UGUI UpdateBatches ms | GC Alloc/frame B | Total Used Memory B | GC Used Memory B | Draw Calls / Batches | Vertices |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| SharedCanvas | 1 | 0.8783 / 1.6664 / 2.4622 | 0.0013 / 0.0017 / 0.0579 | 0.0068 / 0.0102 / 0.0503 | 0.2442 / 0.5630 / 1.5514 | 112 / 112 / 4896 | 168893022 / 169204378 / 169500630 | 5058560 / 5238784 / 5246976 | 7 / 9 / 9 | 4940 / 5616 / 5616 |
| FrequencySeparatedCanvases | 3 | 0.5790 / 0.9182 / 1.8961 | 0.0012 / 0.0017 / 0.0278 | 0.0064 / 0.0121 / 0.0492 | 0.2460 / 0.5568 / 1.6214 | 112 / 112 / 7324 | 170204204 / 170456369 / 170460374 | 6225920 / 6443008 / 6451200 | 11 / 11 / 11 | 4940 / 5616 / 5616 |
| PopupPerCanvas | 4 | 0.62785 / 0.9230 / 510.1579 | 0.0012 / 0.0016 / 0.0657 | 0.0066 / 0.0130 / 0.1133 | 0.23995 / 0.5639 / 3.1547 | 112 / 112 / 7712 | 170708896 / 170869504 / 170965962 | 6623232 / 6680576 / 6684672 | 7 / 9 / 9 | 4940 / 5616 / 5616 |

The frequency-separated arm recorded median/P95/max draws and batches of 11/11/11, while the other arms recorded 7/9/9. This data does not identify the batch-breaking cause; independent UI Profiler inspection is **N/A**, and overdraw capture is **N/A**. Do not infer either from draw or batch counters.

## Measurement boundaries

Startup/resize/close entries are endpoint settling samples (some endpoints contain only one or two fresh frames), not full operation-cost measurements. They do not measure complete construction, layout settling, resize work, teardown, or user-perceived transition time. The 600-frame measured intervals exclude those full costs. Recorder availability and sample coverage are present in the raw JSON; missing values must remain N/A rather than be replaced with zero.

These results do not establish IL2CPP, other Unity versions/platforms/devices/resolutions, longer-duration stability, broad visual UX, physical input acceptance, or a universal Canvas policy. These measurements alone do not complete application integration or human acceptance; separate functional results are in the [P7 verification](validation/ui-system/p7/README.md). No profiler capture of overdraw or batch-breaking reason was taken. These fixtures include binding/context work and recorder overhead; measured GC is not allocation-free and is not isolated to an individual method. The separate interactive sample also allocates diagnostic status strings and is not used as the benchmark. Large Main Thread maxima are retained; their cause was not identified.
