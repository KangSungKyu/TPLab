# 2026-10-15 · 06 · UI 상태와 XML 설명 정합

위험 등급: **T0 · 문서와 XML 주석만**. C# 변경 3파일의 추가·삭제 줄은 모두 `///`이며, 이 줄을 제외한 원문은 기준 main과 동일하다. 실행 코드·API 시그니처·동작·테스트 기대값은 바꾸지 않았다. 사용자 요청에 따라 T0 기본 절차에 더해 컴파일과 전체 EditMode/PlayMode를 각각 한 번 실행했다.

## 기준과 범위

기준 main/source는 `b20f8ca1a12dc77eccbf7cca7fc5eb7da008e553`, 브랜치는 `codex/ui-docs-status-alignment`이다. 앞선 PR #29는 사용자 승인으로 HEAD `807891211aca57c764016023779df83e41cb5b23`의 CI 성공과 base `a09e25b41b4b2c937c48e5df659f33c61a5c3928`를 확인한 뒤 일반 merge했다. 로컬 main·origin/main·서버 main 일치를 확인하고, 그 tip이 main에 포함됨과 다른 worktree 사용 없음·로컬/원격 tip 일치를 확인해 해당 로컬/원격 브랜치만 정리했다. 원격 삭제는 확인한 SHA 조건을 사용했고 로컬은 branch -d였다. 사용자 dirty 6개는 raw SHA256을 보존했다.

상태 사실의 근거는 [수락 기록](../UI_ACCEPTANCE.md#L5)과 [2026-10-12 통합 회고](2026-10-12-03-ui-main-integration.md#L3) 및 그 회고의 [실제 Git 마감](2026-10-12-03-ui-main-integration.md#L11)이다. 해당 기록의 사용자 수락, 후보 Single/Additive consumer 통과, main 통합 완료만 반영했다. 기기 세부·다른 플랫폼·이후 변경 수락은 미확인이다. 이번 작업은 과거 consumer/Player 검증을 다시 실행하거나 새 source의 설치 검증으로 주장하지 않는다.

## 변경 전후

아래는 변경한 문장·XML의 전체 전후다. 긴 P7 단락의 나머지 source SHA·수치·링크는 그대로 보존했다. 사람/AI 문서는 동일한 2026-10-12 수락·통합 사실을 말한다.

| 위치 | 변경 전 | 변경 후 |
|---|---|---|
| [doc/api/UI.md:11](../../doc/api/UI.md#L11) | `` Current source candidate `5af64b96b8d7f1793d26465550077ac1a5f8cd25` Single/Additive consumer revalidation is pending; follow [integration status](../validation/ui-system/integration-20261012/README.md). Main integration is in progress, not complete. `` | `` For the 2026-10-12 integration, source candidate `5af64b96b8d7f1793d26465550077ac1a5f8cd25` passed Single/Additive consumer verification; see the [integration record](../retrospectives/2026-10-12-03-ui-main-integration.md). Main integration is complete. `` |
| [doc/ai/api/UI.md:128](../../doc/ai/api/UI.md#L128) | `` Current source candidate `5af64b96b8d7f1793d26465550077ac1a5f8cd25` Single/Additive consumer revalidation is pending; see [integration status](../../validation/ui-system/integration-20261012/README.md). Main integration is in progress, not complete. `` | `` For the 2026-10-12 integration, source candidate `5af64b96b8d7f1793d26465550077ac1a5f8cd25` passed Single/Additive consumer verification; see the [integration record](../../retrospectives/2026-10-12-03-ui-main-integration.md). Main integration is complete. `` |
| [doc/api/UI.md:13](../../doc/api/UI.md#L13) | `` Current P7 partial evidence and open gates are summarized above; `` | `` Historical P7 results and the 2026-10-12 acceptance/integration status are summarized above; `` |
| [doc/GAME_UI_SYSTEM.md:124](../../doc/GAME_UI_SYSTEM.md#L124) | `` These are not results for the latest candidate. `` | `` These are not results for the 2026-10-12 integration candidate. `` |
| [doc/GAME_UI_SYSTEM.md:124](../../doc/GAME_UI_SYSTEM.md#L124) | `` Current source candidate `5af64b96b8d7f1793d26465550077ac1a5f8cd25` Single/Additive consumer revalidation is underway and has no recorded outcome yet; follow [integration status](validation/ui-system/integration-20261012/README.md). Main integration is in progress and is not complete. `` | `` For the 2026-10-12 integration, source candidate `5af64b96b8d7f1793d26465550077ac1a5f8cd25` passed Single/Additive consumer verification; see the [integration record](retrospectives/2026-10-12-03-ui-main-integration.md). Main integration is complete. `` |
| [doc/ai/api/UI.md:62](../../doc/ai/api/UI.md#L62) | `` Physical/visual acceptance and main approval remain pending. `` | `` The user confirmed the full UI acceptance checklist on 2026-10-12, and main integration is complete ([acceptance record](../../UI_ACCEPTANCE.md), [integration record](../../retrospectives/2026-10-12-03-ui-main-integration.md)). `` |
| [doc/ai/README.md:26](../../doc/ai/README.md#L26) | `` Current source candidate consumer revalidation is pending; main integration is underway. `` | `` The 2026-10-12 integration candidate `5af64b96b8d7f1793d26465550077ac1a5f8cd25` passed Single/Additive consumer verification; main integration is complete ([integration record](../retrospectives/2026-10-12-03-ui-main-integration.md)). `` |
| [doc/ai/README.md:26](../../doc/ai/README.md#L26) | `` Current source candidate consumer revalidation remains pending; main integration is underway. `` | `` The 2026-10-12 integration candidate `5af64b96b8d7f1793d26465550077ac1a5f8cd25` passed Single/Additive consumer verification; main integration is complete ([integration record](../retrospectives/2026-10-12-03-ui-main-integration.md)). `` |
| [Assets/TPLab/UI/Runtime/UIEnums.cs:3](../../Assets/TPLab/UI/Runtime/UIEnums.cs#L3) | `` /// <summary>Keep a reset inactive clone until its owning context ends.</summary> `` | `` /// <summary>         /// Keep the owned clone until its context ends. DeactivateView caches it inactive; renderer-only Reuse         /// keeps it active with owned rendering, raycasters, and the visibility mask blocked.         /// </summary> `` |
| [Assets/TPLab/UI/Runtime/UIEnums.cs:61](../../Assets/TPLab/UI/Runtime/UIEnums.cs#L61) | `` /// <summary>Disable only a dedicated view Canvas or a whole registered host's rendering.</summary> `` | `` /// <summary>Keep the clone active while disabling its dedicated root Canvas and captured raycasters under the owned visibility mask.</summary> `` |
| [Assets/TPLab/UI/Runtime/UIContext.cs:214](../../Assets/TPLab/UI/Runtime/UIContext.cs#L214) | `` /// <summary>Records metadata without loading or creating an instance, including from a display hook.</summary> `` | `` /// <summary>Records metadata without loading or creating an instance, including from a display hook.</summary>         /// <param name="definition">Non-null metadata selecting one live direct prefab or explicit provider key.</param>         /// <exception cref="ArgumentNullException">The definition is null.</exception> `` |
| [Assets/TPLab/UI/Runtime/UIContext.cs:267](../../Assets/TPLab/UI/Runtime/UIContext.cs#L267) | `` /// <exception cref="OperationCanceledException">The caller or owner ended its wait.</exception>         /// <exception cref="InvalidOperationException">The provider returns no live prefab or the mutation boundary is invalid.</exception> `` | `` /// <exception cref="ArgumentException">The definition ID is blank or not registered.</exception>         /// <exception cref="OperationCanceledException">The caller or owner ended its wait.</exception>         /// <exception cref="InvalidOperationException">The prefab is unavailable, or the thread, mutation boundary, or context fault prevents preparation.</exception>         /// <exception cref="ObjectDisposedException">The owner is terminating or its root was destroyed.</exception> `` |
| [Assets/TPLab/UI/Tests/EditMode/VirtualScrollRectEditModeTests.cs:9](../../Assets/TPLab/UI/Tests/EditMode/VirtualScrollRectEditModeTests.cs#L9) | `` /// <summary>Draft contract tests. These should fail against the signature-only P5 stub.</summary> `` | `` /// <summary>Contract tests for the implemented VirtualScrollRect; all 7 cases run in the full EditMode suite.</summary> `` |

## 구현 대조와 변경하지 않은 위치

- AI API 62행의 `Physical/visual acceptance and main approval remain pending.`는 실제로 존재했고 같은 문서 13행 및 수락/통합 기록과 모순되어 정정했다. AI README 26·37행도 실제 pending을 확인했다. 지정 pending 위치 중 미발견으로 변경을 건너뛴 곳은 없다.
- [사람 API:3](../api/UI.md#L3), [AI API:13](../ai/api/UI.md#L13), [AI README:28](../ai/README.md#L28)과 [29](../ai/README.md#L29)는 이미 완료/수락을 정확히 적어 변경하지 않았다. [AI API:112](../ai/api/UI.md#L112)의 P4 당시 `NotRun (P7)`는 바로 뒤에 역사적 기록이라고 명시되어 있어 보존했다. P7 historical 수치·SHA와 당시 실행 제한도 바꾸지 않았다. 실제 user-close의 pending approval은 작업 상태 문구가 아니므로 그대로다.
- Reuse는 [UIHooks.PrepareAsync](../../Assets/TPLab/UI/Runtime/UIHooks.cs#L18)의 renderer-only active/차단 계약을 그대로 옮겼다. DisableCanvasRendering은 [UIPresentation.Validate](../../Assets/TPLab/UI/Runtime/UIPresentation.cs#L182)의 owned clone root 전용 Canvas 요구와 [Hide](../../Assets/TPLab/UI/Runtime/UIPresentation.cs#L121)의 Canvas·raycaster·visibility 차단에 맞췄다. borrowed host 전체를 제어한다는 의미를 제거했다.
- Register의 ArgumentNullException은 기존 null 검사, PrepareAsync의 ArgumentException은 GetDefinition의 blank/unknown ID, ObjectDisposedException은 EnsureCommand의 종료·root 파괴 검사에 근거한다. 검사의 순서·예외 메시지·ParamName·실행 경로는 바꾸지 않았다. 근거: [Register](../../Assets/TPLab/UI/Runtime/UIContext.cs#L220), [PrepareAsync](../../Assets/TPLab/UI/Runtime/UIContext.cs#L271), [GetDefinition](../../Assets/TPLab/UI/Runtime/UIContext.cs#L532), [EnsureCommand](../../Assets/TPLab/UI/Runtime/UIContext.cs#L554).
- 선택 항목 [UIInputSystemAdapterTests:432](../../Assets/TPLab/UI/InputSystem/Tests/PlayMode/UIInputSystemAdapterTests.cs#L432)의 과거 stub용 NotImplementedException catch는 테스트 실행 코드이므로 수정하지 않았다. 변경 여부는 후속 별도 범위다.

## 검증

Windows / Unity 6000.3.18f1 / Test Framework 1.6.0 / Connector 0.4.1 / 기존 Editor PID 3428 / Python 3.14 환경이다. `unity-cli editor refresh --compile --project C:/Users/PC/Projects/TPLab`로 Compilation complete와 exit0을 확인했다. 중간 HTTP idle-channel 경고는 있었으나 컴파일 오류로 끝나지 않았다. 새 Editor로 대체하지 않았다.

기존 도구를 변경하지 않고 아래 두 명령을 각 한 번 실행했다. 추가 outer timeout·강제 종료·실패한 테스트의 재실행은 없다. Edit는 CLI 시도1회다. Play 최초 CLI 호출은 테스트 시작 전 `Error: no Unity instances running`으로 거부되어 기존 runner가 같은 Editor PID3428을 재확인한 뒤 한 번 재호출했다. Play 테스트가 실행된 호출은 exit0이며 두 mode 모두 실제 전체 테스트 실행1회다. 이 발견/재조회 비용도 runner 시간에 포함한다. JSON과 native XML의 case 이름을 확인했다.

```powershell
C:/Python314/python.exe -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode EditMode --output Temp/ui-docs-status-20261015/regression/edit.json
C:/Python314/python.exe -B tools/run_unity_tests.py --project C:/Users/PC/Projects/TPLab --mode PlayMode --output Temp/ui-docs-status-20261015/regression/play.json
```

| 실행 | 통과/전체 | 실패 | skip | runner 초 | XML 초 |
|---|---:|---:|---:|---:|---:|
| EditMode | 398/398 | 0 | 0 | 22.648 | 20.758 |
| PlayMode | 379/379 | 0 | 0 | 15.466 | 9.102 |

PR #29 변경 후 마지막 전체 실행의 Edit398·Play379 이름 집합과 동일하고 증감0이다. VirtualScrollRectEditModeTests도 실제 7/7 통과해 주석의 실행 건수를 확인했다. runner 시간은 준비/CLI/Play 진입 등을 포함하고 XML 내보내기는 제외한다. XML 시간은 테스트 자체 구간이다. 이전 T2 전후 반복 실행을 이번 T0 반복 실행으로 세지 않는다.

기계 대조: 허용 C# 3파일에서 XML 줄을 제거하면 기준 main과 줄 단위로 동일하다. 실행 코드 변경0, 신규 source0, 기존 테스트 단언/attribute 변경0, asmdef·upm·tools·workflow·패키지·버전·CHANGELOG 변경0이다. dirty 6개는 snapshot raw hash가 동일하다. 나머지 추적 파일도 허용 범위 밖 Git diff0이다. PR #29 회고 한 파일은 main 전환 중 Git checkout이 CRLF/LF를 정규화했으며 Git blob/내용은 동일하다. 이 줄끝 변화는 사용자 dirty 파일에 해당하지 않는다.

저장소에 tools/verify_doc_links 계열이 없어 tools는 추가하지 않고 작업 소유 Temp의 읽기 전용 verify_doc_links.py로 추적 Markdown과 신규 회고의 상대 경로·Markdown/source-line anchor를 검사한다. 변경 전 Markdown250개·상대 링크2024개·anchor503개·오류0이며 외부 HTTP1025개는 미조회다. 새 회고·INDEX를 포함한 변경 후 결과와 규칙/공백 검사는 commit 검사 뒤 아래에 확정한다. 전체 working-tree의 기존 InitScene 공백3곳은 범위 밖 dirty이므로 수정하지 않는다.

[작업 브랜치 Actions](https://github.com/KangSungKyu/TPLab/actions?query=branch%3Acodex%2Fui-docs-status-alignment)에서 최종 HEAD의 gate를 별도로 확인한다. CI는 Unity 테스트 증거와 구분하며 PR 본문은 이 회고를 링크한다. 이번 PR의 main 병합은 하지 않는다.

## 미실행과 다음 경계

Player/IL2CPP, 실제 설치/패키지 재생성, 새 consumer 빌드, 물리 입력·시각 수락, 별도 GC/성능 계측, 변형·안전망 테스트 추가는 미실행이다. upm은 릴리스 때 재생성하며 이번 주석 정정에 포함하지 않는다. 추가 계약 결정이 필요한 불확실한 예외 설명은 없었다. UI 패키징·버전·CHANGELOG·추가 안전망으로 범위를 넓히지 않았다.

예전 pending 문장을 현재 상태로 읽지 않도록 날짜와 근거를 연결하되, 과거 source별 검증 수치와 이미 완료를 정확히 적은 위치는 보존했다. 같은 상태 사실이 여러 레퍼런스에 남으면 한 곳의 최신화만으로는 모순이 사라지지 않는다는 점을 확인했다.

원본 JSON/XML·timing·주석 전용 diff 증명·hash·링크 검사 결과는 Temp/ui-docs-status-20261015/에 보존한다. PR 검토와 전후 비교에 필요한 동안 보존하고 수락 뒤 다른 작업 참조가 없을 때 이 작업 소유 목록만 정리한다. 이전 Temp 자료는 수정하거나 삭제하지 않았다.
