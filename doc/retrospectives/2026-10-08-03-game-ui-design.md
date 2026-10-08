# Game UI System 설계 검토

- 날짜/상태: 2026-10-08 / 설계 보완·UIContext 명칭 채택·P0~P7 계획 작성, 구현 미착수.
- 요청: uGUI Canvas·Panel 기반 Popup, Virtual ScrollRect를 제공할 UI 모듈의 로드 시점·프로젝트별 적용 범위를 검토한다. 설계 요청을 구현/배포로 확대하지 않는다.
- 기준: main `8ce768d96f79ed8328143bebc31e77f060f51205`; 작업 branch `codex/game-ui-design`. 선행 [차기 버전 회고](2026-10-07-16-distribution-refinement.md)와 [Core 계획](../CORE_PLAN.md)을 확인했다. 기존 사용자 Unity 변경6개는 보존 대상이다.
- 변경: [GAME_UI_SYSTEM_DRAFT](../GAME_UI_SYSTEM_DRAFT.md), Core 계획·문서 색인에 미구현 제안을 연결한다. runtime·package·설정·배포 사본은 수정하지 않는다.
- 제안: 준비 시점/인스턴스 보관/자산 owner/표시 owner를 구분한다. 기본은 최초 사용 준비와 닫기 후 clone 정리, 필수 공통 화면만 선행 준비, 잦은 화면만 명시적 재사용이다. 공용/scene root 선택과 Inspector/script 등록을 함께 제공한다.
- 핵심 제한: ResourceManager는 개별 key 해제를 제공하지 않으므로 팝업 Destroy가 자산 해제를 의미하지 않는다. Input layer는 uGUI raycast/focus 차단과 별개고 InputSystemUiScope는 Samples API다. PrefabPool.Dispose에서 onReturn이 호출되지 않으므로 최종 UI cleanup은 별도로 수행해야 한다.
- 조사: 부모는 요구/순서/문서와 Unity 공식 문서를 검토했다. `/root/ui_lifecycle_review`는 ResourceManager·Pool·Input·callback 계약을 읽기 전용으로 확인했다. 전체 문맥 상속 모델/추론이며 override 없음. 변경 허용 경로 없음; 상태 완료. 새 사용자 소유 세션이나 외부 메시지를 만들지 않았다.
- 도구 경계: 기본 sandbox의 Git status가 work tree 오류를 반환해 실제 이름 변경 경로에 대한 승인된 확장 실행으로 읽기/쓰기했다. Git 설정·user dirty의 원복 없이 현재 branch/HEAD·소유 범위를 확인했다.
- 검증: 문서5개·로컬 링크200개(누락0)·공백0·허용 범위 검사를 통과했고 코드/사용자 입력371개 aggregate hash가 전후 동일했다 (`569b2d8e5fa53315bcfe4c01ea064c76f8afa20dd70256fd4c5039442ae7a7b7`). Unity 테스트/compile/Player/Profiler는 실행0건; 문서 전용 설계이므로 이번 단위에 실행 대상이 없다. 과거 테스트를 현재 UI 결과로 사용하지 않는다.
- Git: 설계 branch 생성만 수행. 이번 문서는 미커밋이며 commit/push/main 병합·브랜치 삭제를 수행하지 않는다. 확정 전 제안을 현재 배포 API로 표시하지 않는다.
- 다음: 사용자가 기본 정책·scope와 virtual scroll 첫 범위를 검토한 뒤 의존성 분리와 public UI 계약/TDD 단계를 확정한다. 실제 UI, modal·focus·버튼 재전달, cell recycling·비동기 표시, Additive/Single UX는 미검증이다. 이번 설계 요청에는 Unity/PC 종료 지시가 없다.

## 2026-10-08 후속: HUD·modal/modeless·Canvas 최적화 검토

- 사용자 보완: HUD를 상황별로 선택하고 popup을 modal/modeless로 표시·해제, depth/graph 관리. 사용자 배치는 재량으로 두되 Canvas 수준은 공용 모듈이 정책을 제공한다.
- 반영: 동일 초안에 HUD 선택, 단일 부모 owner graph와 표시 순서/Canvas 분리, 가장 위 modal을 입력 경계로 하는 정책, Visible 중 mode 전환, 관리 Canvas host의 제어/검사 범위를 제안했다. 이전의 최상단 popup만 입력 문구를 modal 위 modeless와 일관되게 수정했다.
- 최적화: 변경 빈도별 Canvas 후보, shared host의 기본 batching과 측정 후 isolation, 변경 없는 대입/정렬 억제, 기본 uGUI delayed rebuild, Virtual ScrollRect의 stable cell, pool/숨김/input/update 정리. draw call 최소화와 rebuild/GPU/overdraw의 tradeoff를 기록하며 임의 개선 수치는 제시하지 않는다.
- 조사/분업: 같은 `/root/ui_lifecycle_review`를 재사용해 owner/depth/Canvas 분리, modal 위 modeless, 등록 동결된 Input layer의 lease와 runtime mode, 유효 focus 복원을 읽기 전용 리뷰했다. 모델 override·재위임·허용 경로 변경 없음; 완료. 부모는 Unity 공식 문서와 설치 uGUI 2.0.0 CanvasUpdateRegistry/LayoutRebuilder를 대조했다. 새 rebuild scheduler·Canvas 자동 분할·runtime 코드는 만들지 않았다.
- 브랜치/보존: 동일 `codex/game-ui-design`, 기준 HEAD 동일. 이번 수정 범위는 같은 초안·Core 계획·색인·회고4개이며 이전 회고 색인과 사용자 Unity dirty를 유지한다. 코드/사용자 입력371개 hash를 후속 시작 전 확인했다.
- 검증: 문서5개·로컬 링크201개(누락0)·공백0 검사를 통과했고 코드/사용자 입력371개 hash는 후속 수정 전후 동일했다. Unity 테스트/compile/Player/Profiler 실행0건. 구현 시작·main 병합·커밋/푸시·배포/Unity/PC 종료는 수행하지 않는다.
- 다음: HUD 선택과 graph/state/input 계약을 확정하고 첫 예제에서 공유/빈도별/popup별 Canvas 비교와 실제 batch/layout 비용을 측정한다. 현재 문서는 미구현 설계 제안이며 성능 보장이 아니다.

## 2026-10-08 명확화: HUD 구분 재량·인벤토리 Canvas

- 사용자 선택: static/dynamic HUD를 명시적인 별도 기능으로 제공하지 않고 공통 시스템을 프로젝트가 그렇게 나눠 사용할 수 있게 한다.
- 반영: 공통 HUD/Canvas host와 프로젝트 선택의 Canvas 연결을 명시했다. 기존 표를 필수 host 종류가 아닌 사용자 구성 예로 수정하고 인벤토리의 공유 Canvas/전체 독립 Canvas/목록 child Canvas 선택 조건을 추가했다. modal·owner·depth 계약은 Canvas 선택과 동일하게 유지한다.
- 범위: 같은 설계 branch에서 초안·이 회고2개만 보완. 소스·package·사용자 설정, 이전 문서 변경을 유지하고 구현·Git 통합·Unity 조작은 수행하지 않았다. 작은 명확화라 추가 agent 업무는 만들지 않았다.
- 검증: 설계 문서5개·로컬 링크201개 누락0, 공백0, 코드/사용자 입력371개 hash 수정 전후 동일. Unity 테스트·컴파일·Player·Profiler 실행0건이며 인벤토리 분할의 실제 비용은 미측정이다.

## 2026-10-08 명확화: 활성화 API·Popup 책임·구독 수명

- 사용자 보완: SetActive/Canvas.enabled의 용도와 차이를 API에서 명시하고, Popup의 공용 책임과 프로젝트 통신/비즈니스 로직의 경계를 확인한다.
- 반영: 설명용 DeactivateView/DisableCanvasRendering 전략, 전용 Canvas·공유 root 검증과 관리 Open/Close/Shutdown의 차이를 기록했다. 구독은 기존 event/UnityEvent를 사용하며 해지 작업 등록과 표시/owner token을 제공하는 안이다. 데이터 조회·domain transaction·메시지 전달 자체는 프로젝트가 소유한다.
- 불변 조건: 공유 Canvas를 단일 popup 숨김으로 끄지 않고, 다른 소유자의 listener/입력 lease를 해제하지 않는다. 표시 종료 cleanup은 파괴/Pool.Return에만 의존하지 않으며 callback 실패에도 나머지 정리를 계속 시도하고 오류를 보존한다. UI 종료를 business rollback으로 간주하지 않는다.
- 범위: 기존 설계 branch에서 초안·회고2개만 보완한다. public API 이름은 미구현 제안이다. source/package/설정·기존 dirty를 보존하고 Git 통합·Unity/PC 조작을 수행하지 않는다.
- 검증: 문서5개·로컬 링크201개 누락0·공백0, 코드/사용자 입력371개 hash 보존을 확인했다. Unity 테스트/compile/Player 실행0건; async 취소·event 누출·재표시는 구현 후 TDD 대상으로 남긴다.

- 후속 읽기 전용 리뷰: 동일 ui_lifecycle_review 재사용/완료. 역순·정확히 한 번 cleanup, 빌린 서비스 Dispose 제외, 표시/owner/caller 취소 구분과 종료 재진입·오류 보존을 같은 초안에 보완했다. 부모가 변경 문서의 로컬 링크·공백을 재검사했으며 추가 runtime 실행은 없다.

## 2026-10-08 구현 전 보완 검토

- 요청: 현재 UI 시스템에서 더 보완할 계약을 검토한다. 기능 추가보다 구현 전 결과·실패 계약을 명확히 한다.
- 반영: 접수 순번 기반 표시 순서, Opening/Closing 명령 충돌, 기본 owner/context parent와 A-B-C 중간 종료, 재사용 이전 handle, 사용자 닫기/owner 종료 구분과 닫기 입력 재전달, 실제 Canvas/입력 경계 불일치의 오류 기준을 같은 초안에 권장 제안으로 기록했다.
- 리뷰: 동일 ui_lifecycle_review를 읽기 전용 재사용/완료. C focus 유지, Closing barrier 유지, owner 우선순위·만료 handle, map 유지 중 입력 재전달, 순서 불일치 실패 처리의 미확정 계약5개를 확인했다. 부모가 접수 순번과 최소 관찰/검증 기준을 함께 정리했다. 재위임·새 사용자 소유 세션은 없다.
- 범위: 동일 branch/HEAD에서 초안·회고2개만 보완. API/정책은 미구현 검토 제안이며 commit/push/main 병합·Unity/PC 조작은 수행하지 않는다. 사용자 Unity dirty와 이전 문서 변경은 보존한다.
- 검증: 문서5개·로컬 링크201개 누락0·공백0, source/user 입력371개 hash 전후 동일을 확인했다. Unity 테스트/compile/Player/Profiler 실행0건. 이 절의 동작은 구현 후 Red/Green과 실제 Input/UI 경계 검증을 필요로 한다.

## 2026-10-08 UIContext 명칭·등록/생성·Phase 분할

- 요청/선택: 사용자는 UIContext 명칭이 더 적절하다고 선택하고 추가 검토가 없다면 Phase를 나누도록 요청했다. 도식과 질의응답에서 등록/자산 준비/표시, 생성 책임, root 범위에 따른 영속성의 차이를 확인했다. 구현 요청으로 확대하지 않는다.
- 반영: 같은 초안에 UIContext 수명·내부 생성 경계와 ResourceManager 자산 소유권, Register/선택적 Prepare/Open·HUD 선택의 같은 경로를 정리하고 P0~P7의 범위·선행 조건·완료 gate를 기록했다. 별도 public UIManager/Factory를 필수 추가하지 않는다.
- 분업: ui_lifecycle_review를 읽기 전용 재사용/완료했다. 새 사용자 질문이 필수인 사항은 발견하지 않았으며, P1의 실제 root 종료, P2의 Pool 활성화/표시 허가, P3/P4의 실제 Canvas/입력 검증 분리, P6의 script/Inspector 동일 경로, P5/P7의 ScrollRect/소비/성능 gate를 보완했다. 전체 문맥 상속 모델/추론·override 없음, 변경 허용 경로 없음.
- 기준/보호: codex/game-ui-design / HEAD 8ce768d96f79ed8328143bebc31e77f060f51205를 재사용했고 upstream 없음·원격 미조회다. source/package/설정과 사용자 SceneTemplateSettings 입력371개를 수정 전에 해시로 캡처했다. 이번 canonical 규칙은 path:raw-SHA256 행의 LF 결합이며 aggregate 99b285c8d89918891f5a973332a3c39cc6ec65bd749780fcdf942e254fd843e0이다. 과거 aggregate와 계산 규칙을 혼동하지 않는다.
- 변경 범위: 초안·Core 계획·문서 색인·같은 회고·회고 색인5개만 갱신했다. 기존 사용자 Unity dirty와 이전 설계 문서 변경을 보존한다.
- 검증: 문서5개·로컬 링크202개 누락0·공백0과 허용 변경 범위를 확인했다. source/설정371개 aggregate는 수정 전후 동일하다. Unity 테스트/compile/Player/Profiler는 실행0건이며 계획 문서 전용 단위다. Phase 동작/성능 완료를 주장하지 않는다.
- Git/다음: commit/push/merge·track/Phase branch 생성과 runtime/package 수정은 없다. 구현 요청 이후 P0부터 시작하며 각 Phase에서 실제 TDD·필요 회귀·문서/회고를 수행한다. 최종 사용자 시각/실제 입력 확인은 P7에서 묶고 미확인 시 main/branch 정리를 보류한다. Unity/PC 종료를 수행하지 않는다.

## 2026-10-08 보완: Virtual ScrollRect 대규모 검증

- 요청: 사용자는 기능/성능 테스트에 1,000 단위 등 큰 cell 환경도 포함하도록 지정했다. 기존 Phase 계획의 후속 조건 보완이며 이번에는 UI runtime 구현/실행을 시작하지 않는다.
- 반영: 초안의 Virtual ScrollRect 절과 P5/P7 gate에 필수 N=1,000/10,000, 기본 경계0/1/100, 앞/중간/끝·왕복/점프·count/resize·재사용/늦은 binding을 추가했다. 데이터 항목 수와 활성/보관/현재 총소유/누적 생성 cell 수를 구분한다.
- 성능: 기본 ScrollRect1,000과 가상화1,000을 같은 환경/구간에서 직접 비교하고 가상화10,000은 확장성으로 기록한다. 준비/첫 생성과 warm scroll, UI/데이터 비용을 분리하고 frame time·UI CPU·GC/메모리·batch·cell/생성 수·종료 잔존을 측정한다. 실제 측정 전 FPS/GC0/개선율을 약속하지 않는다.
- 리뷰: 기존 ui_lifecycle_review 읽기 전용 재사용/완료, 전체 문맥 상속·override 없음·수정 허용 경로 없음. 동수 비교와 확장성 구분, 활성+보관 상한과 누적 생성 증가를 보완했다. 재위임·새 사용자 작업 생성은 없다.
- 보호/Git: 같은 codex/game-ui-design / HEAD 8ce768d96f79ed8328143bebc31e77f060f51205, upstream 없음·원격 미조회. 초안·같은 회고2개만 수정하고 기존 사용자 Unity 변경·이전 문서 변경을 보존한다. commit/push/merge·Unity/PC 조작은 없다.
- 검증: 관련 문서5개·로컬 링크202개 누락0·공백0, source/설정371개 raw aggregate 전후 동일(99b285c8d89918891f5a973332a3c39cc6ec65bd749780fcdf942e254fd843e0)을 확인했다. 이번 기능/TDD·Profiler·Player 실행0건이며 실제 대규모 기능은 P5, 성능/최종 확인은 P7에서 수행할 조건이다.
