# P4 입력·연출 검증

2026-10-08. 원본 TPLab Unity6000.3.18f1, Editor PID24376, Connector0.4.1. source `deb222cf6fda752d3e0dd6d22bda67f9d60e9e16`. 기존 Core/Input과 사용자6파일, main/0.0.1 배포를 보존했다.

| 범위 | 실제 결과 | 근거 |
|---|---|---|
| Red Edit / UI Play / InputSystem Play | 실패6+6+5=17, skip0 | [Edit native](red-edit.native.json), [Edit](red-edit.json), [UI](red-core-play.json), [InputSystem](red-input-play.json) |
| 최초 Green 전체 UI | Edit24/24 + Play41/41, 실패0 skip0 | [Edit native](first-green-edit.native.json), [같은 run 정규화](first-green-edit.json), [Play](first-green-play.json) |
| 최종 전체 UI | Edit24/24 + Play41/41, 실패0 skip0 | [Edit native](final-edit.native.json), [Edit](final-edit.json), [Play](final-play.json) |
| 원본 제품 상태 | ready, Console 오류0 | [상태](final-status.txt), [Console](final-console.json) |

최종 Edit run c4c9dd94c6b74ec9be545306d0bcc897, 최종 Play direct CLI34520. 실제 InputSystem queue state/native UI module fixture5개를 포함한다. 기존 UI48개+신규17개이며 전체 Core/Input 회귀나 물리 장치 UX 확인을 뜻하지 않는다.

## 고정 입력과 보정

[Red394개](red-inputs/test-inputs.json), [최초 Green398개](first-green-inputs/test-inputs.json), [최종398개](test-inputs.json)를 별도 고정했다. default Assets/InputSystem_Actions.inputactions 및 .meta를 명시 포함했다. [보호6개](preserved-inputs.json)는 raw bytes unchanged. [커밋 일치](source-commit-match.json)는 보호 SampleInput.inputactions 1개를 제외한397개 입력이 source commit Git blob과 일치함을 확인한다. 보호 파일은 미커밋 사용자 내용 그대로 테스트에 사용됐으며 커밋된 baseline과 동일하다고 주장하지 않는다.

준비 hook의 own focus는 기억 후 Visible에 적용, foreign focus는 ArgumentException으로 테스트 기대값을 보정했다. owned wrapper 도입에 따라 기존 P3 fixture의 native parent/sibling 관측만 변경했다. focus child 파괴, Unbind 뒤 다른 활성 modal lease 유지, owner Shutdown 전 fault BlockAll 유지 관측을 같은17개에 보강했다. 기대값 보정을 별도 Red 실행으로 주장하지 않는다.

최초 Green Edit는 완료된 파일을 잠시 PermissionError로 읽지 못했다. 같은 native run07916bfe3e9a4f88a4ba435cd9d1a7ae의24/24 결과를 검증해 보존했고 테스트 재실행은 없었다. [복구](file-reader-recovery.json)와 self-check 후 도구는 deadline 내 PermissionError 읽기만 재시도한다. 손상 JSON/다른 run은 성공으로 숨기지 않는다. 변경 도구를 포함해 최종65개를 별도 실행했다.

## 원본 Editor 등록 한계

브랜치 이동 뒤 UIPresentation.cs와 기존 P3 테스트2개가 AssetDatabase에는 있으나 compiler registry에서 누락됐다. public ForceImport/Refresh/CleanBuildCache, 대상 SaveAndReimport와 metadata 재직렬화만으로 영구 복구되지 않았다. 이번 소유 meta3개에 추가된 재직렬화 필드는 HEAD bytes로 복원해 GUID를 보존했다. 설치 Unity 내부 registry의 기존 모든경로를 보존한 union에 owned source3개만 추가해 실제 컴파일/Bee.rsp·로드 타입을 확인했다. Red7330→7333, Green7332→7335 뒤 Console0/실제65개를 확인했다. [진단](editor-registration-recovery.json)은 domain reload 이후 enumeration 누락 재발도 기록한다. 영구 복구·정확한 결함 원인·fresh consumer 통과를 주장하지 않는다. 후속 track은 ancestry/CAS 확인 뒤 ref를 이동해 이전 snapshot checkout에 따른 source 삭제·재생성을 피한다.

## 완료와 후속

XML/사람/AI 문서·[회고08](../../../retrospectives/2026-10-08-08-ui-input.md), 입력/GUID/상대링크 확인 후 track에 통합한다. Unity 원본 meta 후행 공백은 보존하고 C#/도구/문서 검사는 별도 수행한다. 소비 프로젝트·graphics Player·Profiler와 실제 입력/시각 UX는P7이다. main 병합/branch 삭제는 최종 사용자 수락을 기다린다.
