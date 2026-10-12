# 2026-10-12 · 개선 백로그·GC 초안 원문 반영과 개정

## 작업과 기준

- 초판 요청: 첨부 두 문서를 원문 그대로 반영하고 PR #4를 생성했다. 개정 요청(2026-10-12): 승인된 새 첨부 두 파일로 교체하고 같은 브랜치에 별도 커밋·일반 푸시한다. 첨부 문서의 내용은 임의로 다듬지 않는다. 구현·main 병합·강제 푸시·브랜치 삭제는 제외하며 지시문 `CODEX_PROMPT_1.md`/`CODEX_PROMPT.md`는 저장소에 넣지 않는다.
- 기준: main/origin/main/server `c1804e17f3f0cdfbcf32066f48685dc952cb220b`; 작업 `codex/docs-backlog`; 초판 커밋 `bbc313a15ed155986aed2d39a857bb8242e06360`을 보존한다. 기존 dirty 6개는 raw SHA256으로 보존하며 stage하지 않는다.
- 상태: 개정 문서 반영·정적 검토, PR 사용자 확인 대기. 백로그의 결정 기록은 제공 자료로 보존하며 이번 문서 개정을 구현·GC 정책 확정·main 병합 승인으로 사용하지 않는다.
- 기준 규칙: [문서화 지침](../DOCUMENTATION_GUIDE.md), [작업 규칙](../../AGENTS.md), [관련 CI 수락 기록](2026-10-12-06-ci-review-acceptance.md).

## 결정과 변경

- [개선 백로그](../IMPROVEMENT_BACKLOG.md)와 [GC 초안](../GC_ALLOCATION_GUIDELINE.md)는 현재 개정 첨부의 bytes와 Git blob에 일치하도록 교체한다. 초판은 첫 커밋에 남기며 개정은 별도 커밋으로 추가한다. 문서/회고 색인을 갱신하고 기존 `UI_PERFORMANCE.md`, 코드·테스트·설정·AGENTS.md는 수정하지 않는다.
- 원문 해시(SHA256): 백로그 `dc87f21fe79a18cb6d91907b378353f80319441f7ef6a1bd0de0b2db8ce0321f`; GC `f69051153984672eaa861a71085ed1e95b9d09beebf64bea80120e57fdeda59c`.
- 개정 후 해시(SHA256): 백로그 `90aa3f7bcbece33f3048b98009db323ba8796d18aff89d43e844205f95521141`; GC `91de2cda6929ddd53d81a14ae5a358a27f39422e3d44e381fc66525734f44580`.
- `/root/ui_dependency_audit`를 읽기 전용으로 재사용하여 대상 위치·UI 경계·성능 문서 대조를 분담했다. 파일/Git/API/Unity mutation은 부모만 수행한다. 구현 하청·Unity 테스트는 없다.

## 문서 헤더와 링크 대조

- 초판과 개정본 모두 UTF-8/LF이고 현재 링크 형식과 일치한다. 초판 두 파일의 상대 링크는 5개였고 개정본 두 파일은 새 CI/UI 문서 링크를 포함해 7개다. 모두 실제 파일로 연결되는지 재확인한다. 지침에는 모든 내부 검토 문서에 강제하는 단일 헤더 템플릿이 없다.
- 초판 GC의 근거 revision·검증 상태 누락을 개정본 5행의 근거 기준/검증 상태/범위 밖 문단으로 보완했다. `de879b9`/main `c1804e17` 대조와 코드 읽기 기준, 실제 할당 미측정, UI 제외를 명시한다. 이는 GC 정책 확정 또는 실제 측정 증거가 아니다.
- 백로그는 `de879b9396ddae0523bd3ab86939b679b383dd92`/v0.0.1과 실행 미확인을 명시한다. 현재 `v0.0.1` tag의 commit도 이 SHA다. 개정 첨부의 상단 안내에 기존 main 대조가 추가되었으며 추가 편집은 하지 않는다. 새로운 사람/AI public API 문서는 필요하지 않다(계약 변경 없음).

## GC 초안과 UI 성능 문서

- 직접 수치 충돌은 발견하지 않았다. GC 초안은 Core/Input의 경로별 제안 정책이고 [UI 성능 기록](../UI_PERFORMANCE.md)은 별도 Windows Mono Player fixture의 과거 측정이다.
- 겹침: 측정 전 최적화 금지, GC와 기능/의미 보존, Editor/Mono/IL2CPP·기기 범위 구분. GC 2/8항은 개별 P0 경로의 할당 0 검사를 제안하지만 성능 기록 17/43/45행은 fixture·바인딩/context·recorder 포함 프레임값이며 메서드별 귀속을 하지 않는다.
- 성능 기록 21–23행의 Virtual1000 GC median 8,560 B/frame, Virtual10000 25,460 B/frame은 원문 그대로 과거 관찰이다. 이 값만으로 특정 Core/UI 메서드의 정상 경로 할당을 단정할 수 없다. 새 측정·원인 분석을 수행하지 않았다.
- 범위 경계: GC 개정본 5행에서 `Assets/TPLab/UI`를 명시적으로 제외했다. UI 적용은 IMP-12 확정 후 별도 IMP-13 요청에서 결정한다. IMP-13은 큐 밖의 읽기 전용 제안이며 기존 성능 기록을 기준선으로 쓰고 새 측정을 하지 않는다. 문서 통합·정책 확정은 하지 않았다.

## 백로그 대상과 현재 main 대조

`git diff --name-only de879b9396ddae0523bd3ab86939b679b383dd92 c1804e17f3f0cdfbcf32066f48685dc952cb220b -- Assets/TPLab/Core Assets/TPLab/Input/Runtime Assets/TPLab/Tests Assets/TPLab/Input/Tests` 결과는 빈 목록이다. 지정된 구현/테스트 파일은 기준 revision 이후 변하지 않았고 경로 누락도 없다. 아래 초판 지적은 코드 이동이 아니라 위치 표기/CI 전제 차이였으며 개정 첨부에서 수정되었다.

| 항목 | 초판 표기 | 현재 사실·개정 내용 |
|---|---|---|
| IMP-11 `InputLayerController.CalculateSnapshot` | 307 | 개정본은 실제 선언 `Assets/TPLab/Input/Runtime/InputLayerController.cs:308`로 수정. `SameState:337`은 유지 |
| IMP-08 `LoadedScene.EnsureMainThread` | 68 | 개정본은 실제 선언 `Assets/TPLab/Core/ResourceManagement/LoadedScene.cs:66`으로 수정. 68은 같은 메서드의 thread 검사 본문 |
| IMP-07 CI 전제 | `tools/README.md`와 helper가 CI 미구성 전제 | 개정본은 PR diff 검사/요청 시 Claude 리뷰를 일부 완료, Unity 테스트 CI는 보류로 구분. `tools/README.md:14`의 과거 설명은 별도 정리 대상으로 남기며 이 PR에서 수정하지 않음 |

다른 대표 지점은 일치: `PrefabPool.Return:92`, `DisposedPoolRejectsRentAndReturn:242`, `RootInstallation.ShutdownOwnedAsync:98–147`, `AsyncSceneRootTests.Wait:53`/종료 취소 테스트 `:360`, `GameSceneEntryTests:78`, `GameSceneManager` 속성 `:69/:72/:78–83`, 기대 씬 `:996`/iterator `:998`/inventory `:1120`, importer `Thread.Sleep(75):180`. `TryReturn`·shutdown timeout은 현재 구현에 없고 원문의 제안/정적 문제 가설로 남아 있다. 이번 대조를 실제 Red/결함 재현으로 표현하지 않는다.

## UI와 항목 범위의 겹침

| 항목 | 현재 UI와의 관계 |
|---|---|
| IMP-01/02 풀 | UI는 자체 clone/cell 재사용을 사용하며 Core ObjectPool/PrefabPool을 직접 호출하지 않는다. 개념은 비슷하지만 동일 수정 대상으로 확장하지 않음 |
| IMP-03 root 종료 | `UIContextInstaller:20`은 SceneRootInstaller이며 Prepare/Release를 구현하므로 공용 root 수명 변경의 소비 경계다. 자체 UIContext 종료 계약까지 임의 변경하지 않음 |
| IMP-04 timeout 조사 | 원문 Core/Input 테스트 경로에는 NUnit `[Timeout]` 0건. 별도 `UI/Tests`, `UI/InputSystem/Tests`는 원문 조사 범위에 없다. UniTask `.Timeout(...)`와 NUnit 속성을 구분해야 함 |
| IMP-05/10 scene manager | UI를 갖는 root의 씬 전환과 간접 연결되지만 원문의 대상은 GameSceneManager. UI 속성/API까지 확대하지 않음 |
| IMP-06/09 importer·테이블 | 지정한 UI runtime 파일과 직접 같은 수정 대상은 없음 |
| IMP-07 CI | 현재 PR diff 검사는 UI 경로도 읽지만 Unity/UI 실행 검증을 제공하지 않는다 |
| IMP-08 중복 helper | 원문의 Core 4곳과 별도로 UI에도 EnsureMainThread가 있다. 전체 저장소 중복이 4곳이라는 근거로 확대하지 않음 |
| IMP-11 Resource/Input | `UIContextInstaller:46/:310`의 선택 ResourceManager provider와 `UIInputSystemAdapter:127`의 `InputManager.Layers.AcquireLayer`가 연결된다. 기본 TPLab.UI는 Input assembly를 참조하지 않으며 연결은 선택 adapter에 있다 |
| IMP-12 GC 정책 | 개정 초안에서 UI는 명시적 범위 밖. 정책 확정·AGENTS 반영은 별도 사용자 결정 |
| IMP-13 UI 점검 | Runtime/InputSystem/Tests와 Samples/UI 대상의 큐 밖 제안. IMP-12 확정 뒤 별도 요청으로 읽기 전용 점검, 새 측정·자동 구현 없음 |

## 검증과 한계

- 문서 전용 diff의 공백·상대 링크·원문 bytes/blob·변경 범위와 `python tools/check_pr_rules.py --base c1804e17f3f0cdfbcf32066f48685dc952cb220b --head HEAD`를 확인한다. 실제 최종 검사 출력과 최종 HEAD Actions URL/결과는 PR 본문과 완료 보고에 기록한다.
- 기존 dirty `InitScene.unity`의 trailing whitespace 3개는 전체 working-tree diff 검사에 보이지만 이번 문서 검사 범위에는 포함하지 않으며 수정하지 않았다.
- Red/Green·EditMode/PlayMode·컴파일·제품 Console·Player·할당 측정은 모두 미실행(코드·자산·설정 변경 없음). 규칙 검사는 Unity 테스트 성공이 아니다.
- 새로운 실동작 검증이나 외부 플랫폼 호환성을 주장하지 않는다. Actions의 정확한 최종 HEAD 결과는 push/PR 이후 확인하며 과거 실행을 재사용하지 않는다.

## 다음 작업

- PR 문서 수락 및 main 병합 승인 대기. 브랜치 삭제·main 병합·강제 푸시는 수행하지 않는다.
- 초판 위치/CI 전제·GC 메타데이터 수정은 이번 개정 첨부로 반영했다. 남은 확인은 개정 PR의 최종 수락·병합 승인, IMP-12 정책 확정/AGENTS 반영과 IMP-13의 별도 요청 범위다.
- IMP-04 조사나 IMP-01~13 구현/정식 UI 점검은 시작하지 않았다. 개정본의 항목별 gate와 해당 요청 범위를 별도로 확인한다.


## 2026-10-12 · PR #4 개정 보완

- 사용자 개정 프롬프트에 따라 첫 커밋을 되돌리지 않고 개정 첨부 2개·색인·이 회고·회고 색인을 같은 브랜치의 별도 커밋에 포함한다. 일반 push만 수행하며 main 병합은 개정 결과의 사용자 확인 후다.
- IMP-13 추가를 INDEX에 반영했다. IMP-12 확정이라는 선행 조건과 큐 밖/별도 요청/읽기 전용/새 측정 없음은 유지한다. 이번 문서 개정을 GC 정책의 확정으로 간주하지 않는다.
- `/root/ui_dependency_audit`를 읽기 전용 개정 대조에 재사용했다. 새 후보의 존재 확인은 문서 대조이며 IMP-13 전체 점검을 수행하거나 구현하지 않는다. 후보 심볼은 현재 소스에 있고 UIContext partial 두 파일은 합계 1,910행이다. 호출 빈도·실제 할당은 측정하지 않았다.
- 새 첨부 bytes·Git blob 일치 2개, 초판 commit/blob 보존, 새 UI_PERFORMANCE.md·CI_SETUP_GUIDE.md 링크를 포함한 상대 링크, 문서 전용 diff·공백·기존 dirty 보존을 재검증한다. 실제 검사/최종 HEAD Actions 결과와 새 commit SHA는 [PR #4](https://github.com/KangSungKyu/TPLab/pull/4) 본문과 완료 보고에 기록한다.
- Red/Green·EditMode/PlayMode·컴파일·제품 Console·Player·할당 측정은 미실행이며 문서 전용 작업의 정적/CI 검사를 런타임 검증으로 표시하지 않는다.
