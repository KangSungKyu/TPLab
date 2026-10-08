# UIContext 구현 track

2026-10-08. 사용자는 [UI 설계](GAME_UI_SYSTEM_DRAFT.md)의 P0~P7 구현 진행을 요청했다. 명칭은 UIContext, 첫 범위는 HUD·modal/modeless Popup·Canvas host·독립 Virtual ScrollRect다.

## 기준과 보호

- 원격 main 및 로컬 main 기준 8ce768d96f79ed8328143bebc31e77f060f51205를 확인했다. 검토된 설계 문서5개만 codex/game-ui-design에서 852ee960a22791404f179441f8e789f1fee335bf로 기록하고 codex/game-ui-track → codex/game-ui-p0-contracts를 생성했다.
- 사용자 변경6개는 [보호 해시](validation/ui-system/p0/protected-inputs.json)의 raw bytes를 보존하고 수정/stage에서 제외한다. 이 작업의 소스는 Assets/TPLab/UI이며 기존 Core/Input 소스 이동·package 재편·upm 사본·버전/tag/Release는 범위에 자동 포함하지 않는다.
- 원본 TPLab Editor PID24376, Unity6000.3.18f1, Connector0.4.1에서 ready·컴파일/업데이트/Play false·InitScene dirty false·Console 오류0을 확인했다. 다른 Editor로 원본 테스트를 대체하지 않는다.
- 부모가 계약·테스트 실행·리뷰·Git·Unity를 맡는다. 테스트 중 모든 source를 동결한다. 각 Phase는 최신 검증 track tip에서 시작해 완료 gate·회고 후 원본 commit을 보존해 track에 통합한다.
- 최종 시각/실제 입력 수락 전 main 병합·branch 삭제를 하지 않는다. 이 요청에는 Unity/PC 종료가 없다.

## Phase 상태

| Phase | branch | 상태 | 완료 기준 |
|---|---|---|---|
| P0 계약/의존성 | codex/game-ui-p0-contracts | 완료 / 문서·정적 검증 | public 결과·취소/handle·owner/host와 실제 의존성 경계 |
| P1 Context 수명 | codex/game-ui-p1-lifecycle | 완료 / 자동 검증·문서·회고 | 실제 Red/Green, root 종료·반복/실패 cleanup·세대 handle |
| P2 등록/준비/생성 | codex/game-ui-p2-loading | 미착수 | direct/provider·runtime lazy·재사용·늦은 완료와 부분 정리 |
| P3 HUD/Popup/Canvas | codex/game-ui-p3-presentation | 미착수 | HUD 보존·owner tree·A-B-C와 실제 정렬/숨김 |
| P4 입력/연출 | codex/game-ui-p4-input | 미착수 | 실제 Input System/EventSystem·modal/focus·lease·재전달 |
| P5 Virtual ScrollRect | codex/game-ui-p5-scroll | 미착수 | 실제1,000/10,000 항목·활성+보관 상한·누적 생성·binding |
| P6 설정/root/예제 | codex/game-ui-p6-integration | 미착수 | Inspector/script 동일경로·Single/Additive·callback 예제 |
| P7 통합/성능/수락 | codex/game-ui-p7-validation | 미착수 | 회귀·소비/Player·동수1,000 성능비교/10,000 확장성·최종 UX |

P0~P4는 순차이며 P5는 P0 계약/P2 재사용 경계 이후 독립 진행 가능하다. P6은 P4/P5, P7은 P6을 선행한다. 문서와 자동 검증은 해당 Phase에서 수행하며 P7까지 미루지 않는다.

## 담당

| 담당 | 선택·목적 | 허용 범위·상태 |
|---|---|---|
| 부모 | 공용 계약·상태 수명·실제 실행·통합 책임 | UI source/관련 테스트·문서/도구만, 기존 사용자 변경 제외 |
| /root/ui_lifecycle_review 재사용 | 문맥 상속 모델/추론 유지, async 수명·재진입의 계약 리뷰 | P0 리뷰 완료; P1 구현/리뷰/최종 UI18+root10 자동 검증 완료; P2 계약 읽기 준비 완료 |
| /root/ui_dependency_audit | gpt-6-luna/low, 명확한 asmdef/설치 의존성 조사 | 의존성/입력 조사와 P1 source 규약 정리 완료; 현재 P1 사람/AI 문서6개 갱신만, Git/Unity·재위임 제외 |

## 현재 증거와 남은 gate

P0는 실제 dependency·[Editor 관찰](validation/ui-system/p0/editor-state.json)과 문서 검증 단위다. Unity 테스트/Player/Profiler 실행0건이며 동작 검증 통과가 아니다. 구현 후 실제 nonzero Red/Green·필요 회귀, 컴파일/제품 Console, source 입력 hash를 Phase별로 기록한다. 최종 사용자 확인은 HUD·A-B-C/자식 종료·실제 입력·1,000/10,000 스크롤·가림막/로딩·화면비·Single/Additive를 묶어 제공한다.

P0 cb82fb427e443f55f72568488bad7869a7510dbe를 track에 fast-forward 통합하고 같은 tip에서 P1 branch를 생성했다. main은8ce768d를 유지한다. P1은 Runtime의UIContext/Handle/정의/요청/hook·enum·root파괴fallback과 Edit9/Play6 실패테스트를 먼저 준비한다. 테스트 수는 예정이며 실제 실행/결과가 아니고, 직접 prefab의 작은 clone은 수명 관측에만 사용한다. provider/공유준비/보관은 P2, Canvas/HUD/depth는 P3, 실제입력은 P4 범위다.

P1 [실제 Red](validation/ui-system/p1/README.md)는 컴파일 오류0 후 Edit9실패/Play6실패·skip0을 같은 Editor에서 확인했다. [Red source 입력](validation/ui-system/p1/red-inputs/test-inputs.json)364개와 보호6개 unchanged를 기록했다. P1 구현 당시 native 파괴 감지를 검토했으며 설치 UniTask의 GameObject destroy token/awake monitor를 재사용했다. 비활성 root 회귀1개는 Green에 추가해 실행했고 별도 Red라고 주장하지 않는다.

P1 source545c342f80061c66ede2fc7f168cfc1c2cb13cee: [최종 근거](validation/ui-system/p1/README.md)의 Refactor Edit9/9+Play9/9 및 기존SceneRoot10/10 실패0/skip0. 종료callback시점·다른popup조합 실제Red2개와보정을포함한다. [회고05](retrospectives/2026-10-08-05-ui-lifecycle.md)를기록했고 문서·증거·회고를 같은 단위로 기록하고 track에 통합한다. 다음 P2는 검증된 P1 tip에서 시작한다. 소비/Player/Profiler/최종UX는P7대기다.
