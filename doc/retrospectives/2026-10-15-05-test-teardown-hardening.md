# 2026-10-15 · 05 · 테스트 종료 대기와 worker 상한 보강

위험 등급: **T2 · 테스트 전용**. 사용자 지정 6개 TearDown, 동기 worker 대기 2곳, VirtualScroll PlayMode 주석 한 줄만 보강했다. 런타임 계약·공개 시그니처는 바꾸지 않았다. 자동 검증 완료 후 PR로 제공하며 main 병합은 사용자 승인 전에는 하지 않는다.

## 범위와 기준

기준 source는 main `a09e25b41b4b2c937c48e5df659f33c61a5c3928`, 브랜치는 `codex/test-teardown-hardening`이다. 변경 전은 이 source와 기존 사용자 dirty 6개가 있는 같은 checkout이며, 변경 후는 여기에 동결한 지정 테스트 9개 변경만 더한 상태다. 기존 dirty 6개는 실행 전후 raw SHA256이 같고 이번 커밋에 포함하지 않는다. source 기준과 최종 문서/commit metadata를 구별한다.

사용자가 이번 T2 범위에 대해 정상본 전후 전체 회귀 각 2회 비교를 지정했고, 종료 대기 보강의 단일 임시 변형은 생략 가능하다고 승인했다. 새 Red·변형은 실행하지 않았으며 runtime 구현 Green이라고 주장하지 않는다. 테스트 삭제·범용 fixture·성능 최적화·reflection 변경·새 패키지·assembly 변경은 없다. 신규 C#·meta 파일도 없어 새 fixture 단독 포함 검증은 비적용이다.

## 변경과 이유

| 파일·위치 | 보강 | 보존한 의미 |
|---|---|---|
| [UIContextPlayModeTests:29](../../Assets/TPLab/UI/Tests/PlayMode/UIContextPlayModeTests.cs#L29) | gate 해제 후 공개 ShutdownAsync 완료를 기다리고 파괴 | 기존 실패 예외 처리와 모든 테스트 단언 |
| [UIContextReuseTests:31](../../Assets/TPLab/UI/Tests/PlayMode/UIContextReuseTests.cs#L31) | source load/gate 해제 → shutdown → native 파괴 | 재사용·부분 실패 기대와 기존 예외 처리 |
| [UIContextPresentationTests:35](../../Assets/TPLab/UI/Tests/PlayMode/UIContextPresentationTests.cs#L35) | 모든 context 종료를 기다린 후 소유 native 파괴 | 기존 callback 실패 예외 처리·borrowed 소유권 |
| [UIContextInputPresentationTests:55](../../Assets/TPLab/UI/Tests/PlayMode/UIContextInputPresentationTests.cs#L55) | gate 해제와 shutdown을 기다린 후 native 파괴 | 기존 오류 관찰과 input/focus 단언 |
| [AsyncSceneRootTests:40](../../Assets/TPLab/Tests/PlayMode/AsyncSceneRootTests.cs#L40) | gate 해제 후 root shutdown, finally에서 native 파괴 | 이미 종료된 root의 관찰된 공유 Aggregate만 허용; 새 정상 root 종료 오류는 전파 |
| [ResourceManagerTests:116](../../Assets/TPLab/Tests/PlayMode/ResourceManagerTests.cs#L116) | root shutdown은 기존 Complete provider pump, 별도 resource shutdown은 기존 Settle로 drain | provider·locator·asset 정리는 완료 뒤 수행; 원래 단언·helper 상한 유지 |
| [SceneLoadProgressTests:103](../../Assets/TPLab/Tests/EditMode/SceneLoadProgressTests.cs#L103) | Task.Wait(3000) 성공 단언 후 원래 결과 관찰 | 예외 타입·callback0·Failure null 기대 유지 |
| [DataTableImporterTests:164](../../Assets/TPLab/Tests/EditMode/DataTableImporterTests.cs#L164) | Join(3000) 결과를 저장하고 파일 정리 뒤 성공 단언 | 기존 75ms worker와 파일/시간/GUID/staging 단언 유지 |
| [VirtualScrollRectPlayModeTests:15](../../Assets/TPLab/UI/Tests/PlayMode/VirtualScrollRectPlayModeTests.cs#L15) | 실행하지 않았다는 주석을 전체 PlayMode에서 실행하는 통합 테스트로 정정 | synthetic PointerEventData이며 physical/Input System 증거가 아니라는 제한 유지 |

6개 TearDown 모두 `IEnumerator` 안에서 `yield return UniTask.ToCoroutine(...)`로 기다린 뒤 **async 본문 밖 마지막에** `LogAssert.NoUnexpectedReceived()`를 수행한다. [과거 No log scope 원인](2026-10-14-09-guard-safetynet-a.md#정상본-실패조건부-승인-정정-이력)을 재도입하지 않았다.

Async root의 예외 필터는 gate 변경 전에 IsReady를 캡처한다. [종료 결과 공유](../../Assets/TPLab/Core/Lifecycle/RootInstallation.cs#L88)와 [Dispose의 IsReady=false](../../Assets/TPLab/Core/Lifecycle/RootInstallation.cs#L299)를 근거로 이미 닫힌 owner의 공유 실패를 재보고하지 않는다. 기존 UI 4곳의 catch 정책은 그대로 유지했다. Resource에는 예외 삼키기를 추가하지 않았다. 두 worker의 3000ms는 기존 Guard-A Join 상한과 같은 규모이며 입력·결과·75ms 작업 자체를 바꾸지 않는다.

Input/EventSystem 검토: UI Presentation 두 fixture는 uGUI EventSystem을 직접 생성하지만 InputTestFixture는 없다. 직접 Input System device/event/runtime을 조작하지 않으며 [기존 UI PlayMode assembly](../../Assets/TPLab/UI/Tests/PlayMode/TPLab.UI.PlayModeTests.asmdef)는 Input System 참조가 없다. 이번 범위에서는 fixture/참조를 추가하지 않았고 async 종료를 기다린 뒤 native를 파괴했다. 나머지 지정 fixture도 Input System 기기·이벤트를 주입하지 않는다.

## 실행과 비교

Windows / Unity 6000.3.18f1 / UTF 1.6.0 / Connector 0.4.1 / 기존 Editor PID 3428 / Python 3.14를 사용했다. 기존 `tools/run_unity_tests.py`는 변경하지 않았다. 각 mode에 고유 output을 지정하여 `baseline/edit-1, play-1, edit-2, play-2`와 `after/`의 같은 순서를 실행했다. JSON과 native XML은 작업 소유 `Temp/test-teardown-hardening-20261015/`에 보존했다. XML은 각 실행의 TestRunner 결과를 SaveResultToFile로 내보내고 JSON과 case 이름 집합을 대조했다.

```powershell
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/test-teardown-hardening-20261015/baseline/edit-1.json
C:/Python314/python.exe tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/test-teardown-hardening-20261015/baseline/play-1.json
```

회차 2와 변경 후 실행은 고유 `baseline/*-2.json`, `after/*-1.json`, `after/*-2.json`을 사용했다. 정확한 8개 명령·시각·CLI PID는 각 phase timing.json에 있다. 추가 outer timeout·Editor 강제 종료·실패를 감추는 재시도는 없다. 편집 직후 refresh의 최초 호출은 domain reload 중 `Error: no Unity instances running`으로 거부됐다. 테스트가 시작된 호출이 아니며 같은 PID 재확인 후 compile 완료를 확인했다. 테스트 8개 호출은 모두 시도1회·CLI exit0이다.

| 실행 | 통과/전체 | 실패 | skip | runner 초 | XML 초 |
|---|---:|---:|---:|---:|---:|
| 변경 전 edit-1 | 398/398 | 0 | 0 | 21.471 | 20.212 |
| 변경 전 play-1 | 379/379 | 0 | 0 | 15.121 | 8.943 |
| 변경 전 edit-2 | 398/398 | 0 | 0 | 21.558 | 20.240 |
| 변경 전 play-2 | 379/379 | 0 | 0 | 14.488 | 8.898 |
| 변경 후 edit-1 | 398/398 | 0 | 0 | 22.423 | 20.562 |
| 변경 후 play-1 | 379/379 | 0 | 0 | 15.002 | 9.065 |
| 변경 후 edit-2 | 398/398 | 0 | 0 | 21.764 | 20.493 |
| 변경 후 play-2 | 379/379 | 0 | 0 | 14.544 | 9.147 |

8회 모두 Edit398·Play379의 이름 집합이 같고 증감0이다. 보강 전후 결과가 같으며 새로 드러난 실패 원문은 없다. VirtualScroll PlayMode7 case도 변경 전후 매회 통과했다. runner 시간에는 준비·Console·CLI·Play 진입/회수 등이 포함되며 XML 내보내기는 제외했다. XML 시간은 UTF 실행 구간으로 별도 계측했다. 현재 반복만으로 미래의 순서 의존성 부재나 장기 안정성을 보장하지 않는다.

## 검사·독립 검토·미실행

- 기존 단언·test attribute 순감소0, 신규 테스트0. 허용 9개 source 외 변경은 이 회고와 회고 INDEX 한 행뿐이다. runtime·asmdef·upm·tools·workflow·패키지·사용자 자산 변경0을 hash/diff로 확인했다.
- 저장소에 `tools/verify_doc_links` 계열은 없다. tools 추가가 금지되어 작업 소유 Temp의 읽기 전용 `verify_doc_links.py`로 추적 Markdown의 상대 경로와 Markdown/source-line anchor를 검사했다. 변경 전249문서·상대링크2010·anchor491, 오류0이다. 변경 후 결과는 아래 검사 확정에 둔다. 외부 HTTP URL 전체를 검사한 것은 아니다.
- 전체 working-tree 공백 검사에서 기존 dirty InitScene.unity의 trailing whitespace3곳이 보였다. 이번 변경 범위 밖이므로 그대로 보존하고 허용 파일/커밋의 공백만 검사한다.
- 하위 에이전트 `/root/ui_closeout_survey`, gpt-6.1-sol/high, 2026-10-15 생성·완료. 공용 계약·수명·검증 상태 분류를 위한 읽기 전용 UI 조사와 이 9개 diff의 독립 정적 검토를 담당했다. 쓰기·Git 변경·Unity 조작0. 지정 변경의 결함·조건 위반을 발견하지 못했으며 Unity 실행 결과는 부모 증거와 구분했다. UI 마감 조사 결과는 요청대로 채팅에만 보고한다.
- [이 작업 브랜치의 Actions](https://github.com/KangSungKyu/TPLab/actions?query=branch%3Acodex%2Ftest-teardown-hardening)에서 최종 HEAD의 실행을 별도로 확인한다. 로컬 규칙 검사·exact-SHA CI의 실제 판정은 검사 확정과 최종 보고에서 구분한다. Actions는 Unity 실행 증거가 아니다.
- 미실행: Player/IL2CPP, 별도 GC/성능 계측, physical 입력·화면 시각 수락, 변형 실험. 공개 계약 불변이므로 XML/API 문서 갱신은 비적용이며 승인한 VirtualScroll 테스트 주석 한 줄만 정정했다. main 병합은 하지 않는다.

## 배운 점과 후속 경계

Dispose 호출이나 고정 프레임 대기만으로 native/owner 종료 완료를 대신하지 않는다. 테스트의 기존 실패 관찰은 유지하면서 공개 shutdown completion을 기다리고, LogAssert는 UTF가 log scope를 연 iterator에서 실행한다. worker 상한은 대기 성공 단언과 정리까지 함께 두어 hang을 명확한 실패로 바꾼다. 이번 8회에서 새 문제는 없었으며 UI API·배포·추가 안전망 후보는 별도 사용자 범위 결정 뒤에만 진행한다.

## 검사 확정

테스트 변경이 들어 있는 source commit은 `1b5b9953ec26f33a78f7212b1b1c8f83ccbfceed`다. 그 뒤 이 검사 요약만 더하며 동결한 9개 C# 파일은 바꾸지 않는다. 전후 8회 case 이름 집합 동일·증감0, 신규 source0, 기존 dirty6과 허용 범위 밖 파일 raw hash 동일을 확인했다.

변경 후 Markdown250개·상대 링크2024개·anchor503개를 검사해 링크/anchor 오류0, 새 오류0이었다. 외부 HTTP1025개는 조회하지 않았다. `git diff --check`를 이번 11개 파일/커밋에 적용해 오류0, `tools/check_pr_rules.py --base a09e25b41b4b2c937c48e5df659f33c61a5c3928 --head HEAD`는 변경11개·오류0·경고0이었다. 보호 파일 warning도 없다. 최종 문서 tip에도 같은 검사를 재적용하고, exact-SHA Actions 결과와 PR은 최종 보고에서 확인한다.

원본 JSON/XML·timing·보존 hash·링크 집계는 작업 소유 `Temp/test-teardown-hardening-20261015/`에 있다. 전후 비교 재검토를 위해 해당 결과와 조사 스크립트는 이번 작업에서 보존하며, PR 수락 뒤 다른 작업이 참조하지 않을 때 이 작업의 명시 목록만 별도 정리한다. 과거 다른 Temp와 기존 캐시는 건드리지 않았다.
