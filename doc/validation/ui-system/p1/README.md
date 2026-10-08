# P1 UIContext 수명 검증

2026-10-08. 기준 track cb82fb427e443f55f72568488bad7869a7510dbe, 작업 branch codex/game-ui-p1-lifecycle. 원본 Editor PID24376 / Unity6000.3.18f1 / Connector0.4.1을 명시적으로 선택했다.

## 실제 Red

컴파일 완료·제품 Console 오류0 확인 후 동일 Editor에서 실행했다. 타입 누락/컴파일 실패가 아닌 stub 동작과 누락된 입력 검증의 실패다.

| 결과 | 실행 | 통과 | 실패 | skip | CLI PID |
|---|---:|---:|---:|---:|---:|
| [EditMode](red-edit.json) | 9 | 0 | 9 | 0 | 6968 |
| [PlayMode](red-play.json) | 6 | 0 | 6 | 0 | 9036 |

[Red source 입력](red-inputs/test-inputs.json) 364개 및 [보호 입력](red-inputs/preserved-inputs.json) 사용자6개 unchanged를 기록했다. 테스트 중 source는 동결했다.

대상은 즉시 handle/반복 await, 정상·취소·실패 cleanup, caller wait 취소, callback 동기 재진입, 비활성 clone 준비, native clone Destroy 완료, root 파괴 fallback이다. Provider/cache/reuse·HUD/Canvas/input·Virtual ScrollRect는 이 결과의 검증 범위에 포함되지 않는다.

## Red 당시 남은 검증

P1 Green 구현·실제 nonzero 통과·관련 root 회귀·소스 입력 대조·제품 Console 확인 대기. 소비 프로젝트/Player/Profiler/최종 시각·실제 입력은 P7에서 별도로 확인한다.

## 실제 Green·리뷰·Refactor

SourceRevision: 545c342f80061c66ede2fc7f168cfc1c2cb13cee. 직접 prefab의 modeless Popup 수명만 구현했다. provider/reuse/HUD/parent/Canvas/modal/Virtual 정책은 명시적으로 NotSupported이며 후속 Phase 대상이다.

| 최종 결과 | 실행/통과 | 실패 | skip | CLI PID |
|---|---:|---:|---:|---:|
| [Refactor EditMode](refactor-edit.json) | 9/9 | 0 | 0 | 22560 |
| [Refactor PlayMode](refactor-play.json) | 9/9 | 0 | 0 | 5844 |
| [기존 SceneRoot 회귀](root-regression-play.json) | 10/10 | 0 | 0 | 14972 |

모두 원본 Editor24376에서 실행했다. [현재 입력](test-inputs.json)364개·[보호6개](preserved-inputs.json) unchanged를 확인했다. 최종 테스트 후 XML 매개변수 설명과 새 folder.meta의 공백만 보완했으며 실행 동작/GUID는 바꾸지 않았다.

첫 구현은 [Edit7/9](green-edit.json)·[Play5/7](green-play.json)이었고 취소4개에서 Task.IsCanceled 기대가 실패했다. 설치 UniTask2.5.11의 AsTask는 OperationCanceledException도 Task.SetException으로 변환한다. 테스트를 실제 await의 취소 예외와 UniTaskStatus.Canceled 관찰로 보정해 [Edit9/9](corrected-edit.json)·[Play7/7](corrected-play.json)을 통과했다. 성공값으로 취소를 숨기는 변경은 하지 않았다.

추가 리뷰에서 종료 callback이 StateClosing/활성 view를 보던 문제와 다른 popup 조합까지 막던 전역 guard를 [실제 Red](review-red-play.json)의 새2개 실패로 재현했다(기존7개 통과). native 정리→StateClosed/Viewnull→Closed observer→공유 완료 순서와 자기 handle/owner 종료·cleanup/native guard의 범위를 고쳐 [Edit9/9](final-edit.json)·[Play9/9](final-play.json)을 통과했다. 이후 private/Allman 규약 정리 후 위 최종18개를 다시 실행했다.

복수 이름을 세미콜론으로 묶은 CLI 필터1회는 total0으로 거부되었다. 이는 Red/통과가 아니며 지원하는 class 필터로 새 검증을 실행했다. 최초 Green Edit 호출은 테스트 시작 전 discovery gap1회만 같은 Editor를 재발견했으며 execution.attempts에 보존했다.

[Console 기록](console-expected-errors.json)의2개 예외는 SceneRootTests의 LogAssert.Expect에 대응하는 의도된 실패 경로 로그이고 미예상 오류0이다. [Editor 관찰](editor-state.json)은 같은 프로젝트·컴파일/업데이트/Play false·InitScene dirty false를 확인했다. core PDB의 과거 경로 표기는 당시 캐시 진단 원문으로 보존한다.

소비 프로젝트/Player/Profiler/최종 시각·실제 입력 검증은 미실행이다. P1 테스트 통과로 전체 UIContext나 다른 Unity/플랫폼 호환성을 주장하지 않는다. after-await 자기 Opened/Closed 순환 대기는 금지 사용이며 자동 검출하지 않는다.
