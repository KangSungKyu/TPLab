# 04 · 할당과 GC

[현행] SourceRevision: main `af084e0d0027c4098fad08c336ccb0753e1c7862`. 갱신일: 2026-10-15. 현재 지침·인용을 읽기/Git 조회로 대조했으며 Unity·테스트·Player 실행은 0회다. 기존 실행·집계 수치는 각 기록의 당시 source 기준이며 이번 재실행·재집계로 보지 않는다. [공통 근거와 검증 범위](README.md#근거와-검증-범위).

## 현재 측정 계약

- [현행] 테스트용 GcAllocationMeasurement는 GC.Alloc recorder에 CollectOnlyOnCurrentThread를 지정하고 sample.Count를 합산한다; 반환0은 그 구간의 관측 이벤트0이지 정확한 byte0 또는 모든 플랫폼의 무할당 보장이 아니다. 근거: [helper:14,76,167](../../Assets/TPLab/Tests/Fixtures/GcAllocationMeasurement.cs#L14), [IMP-14:11,55](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-14/README.md#L11).
- [현행] 같은 Measure 안에서 별도 구간의 음성0·static에 보존한4KB 양성>0을 먼저 확인하고, 미지원·양성 미검출·음성 오염·capacity 소진은 실패한다; caller workload warm-up은 helper가 대신 하지 않는다. 근거: [helper:96,105,160,195](../../Assets/TPLab/Tests/Fixtures/GcAllocationMeasurement.cs#L96), [MissingPositiveControlFailsBeforeWorkloadRuns](../../Assets/TPLab/Tests/EditMode/GcAllocationMeasurementTests.cs#L46).
- [현행] delegate 준비·대조·단언·정리는 workload 구간 밖이며 workload는1회 실행한다; 동기·비중첩·비동시 test-only 계약이고 GC Allocated In Frame은 전역 보조값이다. 근거: [helper:41,50,110,136](../../Assets/TPLab/Tests/Fixtures/GcAllocationMeasurement.cs#L41), [IMP-14:13,14](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-14/README.md#L13).
- [현행] IMP-14의 기존 Windows Mono Editor 실행은 Edit/Play 각각 양성100/100·음성0/100, worker4KB current-thread0events를 기록했다; 작은 할당 크기별 감도·Player/IL2CPP 검증은 이 결과에 포함하지 않는다. 근거: [반복 표:48](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-14/README.md#L48), [worker 검증](../../Assets/TPLab/Tests/Fixtures/GcAllocationControls.cs#L55).

## 실제 적용과 아직 남은 예외

- [현행] pool의 교체된3개 테스트는 충분히 warmed된1000회에서0events를 기록하며, 과거 양성 대조 없는 counter0B는 해당 Editor의 무할당 근거로 정정됐다; pool의 반환·지연 회수 로직 결론은 바뀌지 않았다. 근거: [IMP-14 전후 표:33](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-14/README.md#L33), [계측 정정:67](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-14/README.md#L67).
- [현행] IMP-05는 CanProceed100회2300events에서0으로 바꿨고 충분한 caller capacity의 Count/버퍼 API도0events를 기록했다; 기존 OwnedScenes/RegisteredScenes는 호환성을 위해 allocating snapshot으로 남았다. 근거: [IMP-05:57,61,65](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-05/implementation/README.md#L57), [버퍼 의미:13](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-05/implementation/README.md#L13).
- [현행] All의 기여200events는 original IL 순서 proxy이고 Expected700·Inventory1400·전체2300은 별도 구간이다; 이 합산을 모든 graph의 비중이나 CPU 개선으로 일반화한 측정은 없다. 근거: [측정 경계:53,63,65](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-05/implementation/README.md#L53).
- [현행] 전환/레이어/로드 경로에는 여전히 LINQ·배열·취소 등록 후보가 남고 P1 감사는 측정 후 결정할 백로그다; UI 적용은 IMP-12 뒤 IMP-13 범위다. 근거: [IMP-11:293](../IMPROVEMENT_BACKLOG.md#imp-11-할당-감사-후속-전환이벤트-경로-p1), [IMP-13:333](../IMPROVEMENT_BACKLOG.md#imp-13-ui-구조할당테스트-점검).

## 기존 GC 초안과의 충돌: IMP-12 입력

| 상태 | 기존 문서 위치 | 현재와 다른 부분 |
|---|---|---|
| [현행] | [§8:72](../GC_ALLOCATION_GUIDELINE.md#8-측정과-테스트) | GC.GetAllocatedBytesForCurrentThread 권고는4KB 양성도0을 보고했던 Editor 관측과 충돌한다; “잡음”만의 문제가 아니었다. 근거: [IMP-14 Red:23](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-14/README.md#L23). |
| [현행] | [§8:73](../GC_ALLOCATION_GUIDELINE.md#8-측정과-테스트) | 프레임 GC 값은 현재 helper에서 보조이고 current-thread 이벤트가1순위다. 근거: [IMP-14:11,14](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-14/README.md#L11). |
| [현행] | [§10:89](../GC_ALLOCATION_GUIDELINE.md#10-알려진-위반-이-revision-기준) | CanProceed의 해결 전 목록은 낡았다; Owned/Registered snapshot 할당은 의도적으로 유지한 별도 계약이다. 근거: [IMP-05:13,61,65](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-05/implementation/README.md#L13). |
| [현행] | 근거 revision:5 | 구형 revision·코드읽기 범위이며 최신 IMP-14/05 실행으로 동일하게 검증됐다는 뜻이 아니다. 근거: [IMP-14:3](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-14/README.md#L3), [IMP-05:23](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-05/implementation/README.md#L23). |

- [결정됨] **D-H8 — 신규 API의 조건부 0 events.** 조건부 0 events 규칙은 신규 API에만 적용하고 기존 snapshot형 API의 할당 예외는 분리해 명시한다; 이 결정으로 기존 pool/CanProceed 테스트·계측 결과를 완화하거나 기존 snapshot API를 제거하지 않는다. 근거: 사용자 2026-10-13 일괄 승인, [사용자 결정](README.md#사용자-결정), [현행 snapshot/buffer](02-public-api.md#snapshot과-호출자-버퍼).
- [결정됨] 검토했으나 채택하지 않은 대안: 기존 allocating snapshot까지 새 0 events 규칙의 적용 대상으로 삼거나 snapshot 속성을 메서드로 추가·교체하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H8), [기존 GC §3](../GC_ALLOCATION_GUIDELINE.md#3-공개-api-규칙).
- [결정됨] **D-H8 — 측정 helper 유지.** 현행 GcAllocationMeasurement를 유지하고 소량 할당 양성 대조 추가는 4단계 검증 인프라 항목으로 남긴다; 정확 byte·작은 할당 감도·Player/IL2CPP를 확인한 것으로 쓰지 않는다. 근거: 사용자 2026-10-13 일괄 승인, [helper 결과형](../../Assets/TPLab/Tests/Fixtures/GcAllocationMeasurement.cs#L12), [IMP-14 한계:55](https://github.com/KangSungKyu/TPLab/blob/af084e0d0027c4098fad08c336ccb0753e1c7862/doc/validation/imp-14/README.md#L55).
- [결정됨] 검토했으나 채택하지 않은 대안: 이번 단위에서 backend별 byte 계측을 새로 채택하는 안. 근거: 사용자 2026-10-13 일괄 승인(D-H8), [현행 계측 계약](#현재-측정-계약).
- [현행] 기존 GC_ALLOCATION_GUIDELINE.md를 수정하거나 §11 문구를 AGENTS에 적용하는 작업은 이번 단계에 없다. 근거: [IMP-12 확정 gate:314](../IMPROVEMENT_BACKLOG.md#imp-12-gc-가이드라인-확정), [이번 범위](README.md#사용자-결정).
