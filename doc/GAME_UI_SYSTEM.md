# UIContext 계약

2026-10-08 P0. 구현 승인된 첫 범위의 계약이다. 동작 구현/검증 상태는 [track](GAME_UI_SYSTEM_TRACK.md)에서 확인한다. 0.0.1 배포 API에 포함하지 않는다. 설계 근거는 [초안](GAME_UI_SYSTEM_DRAFT.md)이다.

## 모듈과 수명

- namespace/assembly는 TPLab.UI, source는 Assets/TPLab/UI다. UIContext는 해당 root의 상태를 소유하며 별도 필수 UIManager/Singleton/공개 Factory를 제공하지 않는다.
- 공용 Context는 실제 영속 root, scene Context는 해당 scene root에 연결한다. 논리 부모가 없는 표시의 owner는 Context이고 특정 HUD/Popup 자식으로 요청한 표시는 그 표시의 수명을 따른다. Canvas/Transform 배치와 논리 parent를 혼동하지 않는다.
- Context는 UI clone/표시 작업·자신의 구독/입력 lease를 소유하고 root, prefab 자산, ResourceManager/InputManager/카메라/Canvas/EventSystem을 빌린다. root의 종료는 graceful ShutdownAsync로 기다리고 Unity 파괴는 최종 fallback으로 준비 취소·소유 정리를 시도한다.
- 동작은 Unity 메인 스레드에 한정한다. 현재 표시 handle은 Context+표시 세대로 식별하며 재사용 clone의 이전 handle은 새 표시를 변경하지 않는다.

## 등록·준비·표시 진입

정의는 안정적인 ID·HUD/Popup 역할·직접 prefab 또는 명시적 자산 key·대상 host·보관/숨김 기본 정책을 담는다. 동시에 두 자산 source를 지정하거나 살아 있는 인스턴스를 설정 asset에 저장하지 않는다. 같은 owner·정의의 동시 열기는 기본 거부다.

- Register: 정의·host·정책 검증/등록. 모든 자산 preload/instance 생성을 자동 수행하지 않는다.
- PrepareAsync: 자산의 선택적 선행 준비. 직접 prefab은 이미 준비된 자산이다. 인스턴스 warm-up/표시/modal 차단을 의미하지 않는다.
- BeginOpen: 접수 시 순번을 예약하고 즉시 표시 handle을 반환한다. handle.Opened로 준비·열기 연출의 완료를 기다리며 Opening 중에도 같은 handle의 CloseAsync를 호출할 수 있다.
- OpenAsync: BeginOpen과 동일 경로를 사용하고 열기 완료 후 handle을 반환하는 편의 진입이다. 별도의 생성/취소 경로를 만들지 않는다.
- SelectHudAsync: 다음 HUD의 자산/데이터 준비 후 기존 HUD/그 자식 표시를 교체한다. 준비 실패 시 기존 HUD를 유지하고 종료 실패를 성공으로 숨기지 않는다.
- CloseAsync/ShutdownAsync: 표시/owner의 종료 결과를 공유하고, caller 대기 취소가 시작된 정리를 중단하지 않게 한다. Shutdown은 Context에 신규 요청을 거부시키고 자식부터 종료한다.

메서드별 실제 signature는 구현 source/XML과 사람/AI API 문서에 기록한다. Open caller 취소는 해당 표시 요청과 부분 정리를 대상으로 한다. Prepare caller 취소는 해당 대기를 취소하며 borrowed ResourceManager/native owner를 종료하지 않는다. 이미 종료된 표시의 Close는 그 표시의 결과를 관찰하고 새 표시를 닫지 않는다.

## 표시 수명과 실패

표시 준비/열기/닫기 hook은 프로젝트의 데이터 연결·연출을 기다리는 경계다. RegisterCleanup(Action)에는 이번 표시의 정확한 listener 해지/소유 subscription 정리만 등록한다. 표시는 새 token/세대이며 종료 시 역순·한 번 정리한다. 비동기 종료는 await hook에서 수행하고 async void cleanup을 등록하지 않는다.

Opening→Close는 작업 취소와 부분 정리, Closing→Close는 같은 종료 결과 공유다. Closing 중 재Open/표시 변경, 자기 hook에서 자기 작업 재대기/종료 재진입은 거부한다. owner 종료는 사용자 Cancel/바깥 클릭 닫기 거부보다 우선한다. 실패한 hook/정리 뒤에도 남은 자원을 정리하고 오류를 보존한다.

UIContext의 준비 완료와 root 전체의 준비 완료는 구분한다. SceneRoot installer는 필수 자산/첫 HUD만 준비하고 정상 해제에서 Context Shutdown을 await한 뒤 외부 ResourceManager를 종료한다. UIContext가 scene 로드·업무 규칙·Time.timeScale을 소유하지 않는다.

## 순서·입력·Canvas

한 Context의 기본 HUD는 하나이며 Popup은 같은 표시 영역의 요청 접수 순번으로 정렬한다. 아직 준비 중인 표시는 실제 입력 경계에 참여하지 않는다. 논리 자식은 부모보다 앞에 있어야 하고 실제 host/sibling/sorting으로 표현할 수 없는 순서는 요청 단계에서 거부한다. native UI를 batch 개선 목적으로 임의 재배치하지 않는다.

가장 위 유효 Modal 아래의 UI는 막고 그 Modal 이상은 포인터 후보가 될 수 있다. navigation/submit/cancel focus는 한 곳이며 유효한 C focus를 B 닫기 후 과거 A로 무조건 바꾸지 않는다. Closing Modal 차단은 실제 숨김/정리 완료까지 유지한다. 독립 A-B-C에서 B만 닫으면 A/C 유지, C가 B 자식이면 C부터 종료한다.

숨김 기본은 DeactivateView다. DisableCanvasRendering은 해당 UI 전용 Canvas/whole-host만 허용하고 단일 Popup 때문에 공유 Canvas를 끄지 않는다. rendering을 끄는 것과 입력/focus/표시 작업 정리를 동일하게 안내하지 않는다.

Input System 연동은 별도 선택 경계이며 Core/Input에서 UI를 역참조하지 않는다. 기존 Input layer의 등록 동결과 독립 lease를 유지한다. 같은 닫기 click/submit/cancel이 뒤 UI에 재전달되지 않도록 해제+frame 경계를 검사하고 다른 Modal/전환 차단을 해제하지 않는다.

## Virtual ScrollRect

독립 세로/고정 높이/한 열·한 prefab cell 컴포넌트다. 기존 ScrollRect 동작을 사용하며 data count·bind/unbind는 프로젝트가 제공한다. cell의 이전 binding은 반환 시 정리하고 늦은 비동기 결과는 token/세대로 거부한다.

필수 N=1,000/10,000 기능 gate와 기본 ScrollRect1,000↔virtual1,000의 성능 비교, virtual10,000의 확장성은 [대규모 검증 조건](GAME_UI_SYSTEM_DRAFT.md)에 따른다. 활성/보관/총소유·누적 생성 수를 구분해 안정된 스크롤의 생성 증가를 검사한다.

## 의존성과 구현 경계

기본 UI runtime 참조는 TPLab.Core·UniTask·UnityEngine.UI다. uGUI2.0.0을 제공해야 한다. 현행 단일 Core 때문에 UniTask/Addressables/CsvHelper의 설치 부담이 전이되며 이를 숨기지 않는다. Pool 코드의 직접 외부 plugin 의존과 Core 전체 설치 의존은 다르다.

Input adapter는 UI runtime에서 분리하고 TPLab.Core.Input·Unity.InputSystem·UnityEngine.UI를 참조하는 선택 경계로 구현한다. passive 목록의 UI base는 Input assembly를 참조하지 않는다. 기존 Core/Input 재편·새 배포 package/version은 이번 UI 기능의 암묵적 선행 조건으로 추가하지 않는다.

현재 확인 환경은 Unity6000.3.18f1, UniTask2.5.11, uGUI2.0.0, InputSystem1.19.0, Addressables2.9.1이다. 실제 원본 테스트·소비/Player·성능·최종 수락은 아직 미실행이며 다른 Unity/플랫폼 지원을 주장하지 않는다.
