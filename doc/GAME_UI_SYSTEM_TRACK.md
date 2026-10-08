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
| P2 등록/준비/생성 | codex/game-ui-p2-loading | 완료 / 자동 검증·문서·회고 | direct/provider·runtime lazy·재사용·늦은 완료와 부분 정리 |
| P3 HUD/Popup/Canvas | codex/game-ui-p3-presentation | 완료 / 자동 검증·문서·회고 | HUD 보존·owner tree·A-B-C와 실제 정렬/숨김 |
| P4 입력/연출 | codex/game-ui-p4-input | 완료 / 자동65개·문서·회고 | 실제 Input System/EventSystem·modal/focus·lease·재전달 |
| P5 Virtual ScrollRect | codex/game-ui-p5-scroll | 미착수 | 실제1,000/10,000 항목·활성+보관 상한·누적 생성·binding |
| P6 설정/root/예제 | codex/game-ui-p6-integration | 미착수 | Inspector/script 동일경로·Single/Additive·callback 예제 |
| P7 통합/성능/수락 | codex/game-ui-p7-validation | 미착수 | 회귀·소비/Player·동수1,000 성능비교/10,000 확장성·최종 UX |

P0~P4는 순차이며 P5는 P0 계약/P2 재사용 경계 이후 독립 진행 가능하다. P6은 P4/P5, P7은 P6을 선행한다. 문서와 자동 검증은 해당 Phase에서 수행하며 P7까지 미루지 않는다.

## 담당

| 담당 | 선택·목적 | 허용 범위·상태 |
|---|---|---|
| 부모 | 공용 계약·상태 수명·실제 실행·통합 책임 | UI source/관련 테스트·문서/도구만, 기존 사용자 변경 제외 |
| /root/ui_lifecycle_review 재사용 | 문맥 상속 모델/추론 유지, async 수명·재진입의 계약 리뷰 | P0 리뷰 완료; P1 구현/리뷰/최종 UI18+root10 자동 검증 완료; P2 구현/리뷰/UI30 자동 검증 완료; P3 구현/리뷰/UI48 자동 검증 완료; P4 Temp 테스트17개 준비(미실행) |
| /root/ui_dependency_audit | gpt-6-luna/low, 명확한 asmdef/설치 의존성 조사 | 의존성/입력 조사와 P1 source 규약 정리 완료; P2 문서 완료; P5 Temp tests/signature 초안14개 준비 완료(미실행); P7 Temp benchmark2개 초안 완료(미컴파일/미실행); 현재 P4 native 입력 경계 읽기 검토, Assets/Git/Unity·재위임 제외 |

## 현재 증거와 남은 gate

P0는 실제 dependency·[Editor 관찰](validation/ui-system/p0/editor-state.json)과 문서 검증 단위다. Unity 테스트/Player/Profiler 실행0건이며 동작 검증 통과가 아니다. 구현 후 실제 nonzero Red/Green·필요 회귀, 컴파일/제품 Console, source 입력 hash를 Phase별로 기록한다. 최종 사용자 확인은 HUD·A-B-C/자식 종료·실제 입력·1,000/10,000 스크롤·가림막/로딩·화면비·Single/Additive를 묶어 제공한다.

P0 cb82fb427e443f55f72568488bad7869a7510dbe를 track에 fast-forward 통합하고 같은 tip에서 P1 branch를 생성했다. main은8ce768d를 유지한다. P1은 Runtime의UIContext/Handle/정의/요청/hook·enum·root파괴fallback과 Edit9/Play6 실패테스트를 먼저 준비한다. 테스트 수는 예정이며 실제 실행/결과가 아니고, 직접 prefab의 작은 clone은 수명 관측에만 사용한다. provider/공유준비/보관은 P2, Canvas/HUD/depth는 P3, 실제입력은 P4 범위다.

P1 [실제 Red](validation/ui-system/p1/README.md)는 컴파일 오류0 후 Edit9실패/Play6실패·skip0을 같은 Editor에서 확인했다. [Red source 입력](validation/ui-system/p1/red-inputs/test-inputs.json)364개와 보호6개 unchanged를 기록했다. P1 구현 당시 native 파괴 감지를 검토했으며 설치 UniTask의 GameObject destroy token/awake monitor를 재사용했다. 비활성 root 회귀1개는 Green에 추가해 실행했고 별도 Red라고 주장하지 않는다.

P1 source545c342f80061c66ede2fc7f168cfc1c2cb13cee: [최종 근거](validation/ui-system/p1/README.md)의 Refactor Edit9/9+Play9/9 및 기존SceneRoot10/10 실패0/skip0. 종료callback시점·다른popup조합 실제Red2개와보정을포함한다. [회고05](retrospectives/2026-10-08-05-ui-lifecycle.md)를기록했고 문서·증거·회고를 같은 단위로 기록하고 track에 통합한다. 다음 P2는 검증된 P1 tip에서 시작한다. 소비/Player/Profiler/최종UX는P7대기다.

P1 문서/증거69b0826를 track에 fast-forward 통합한 뒤 같은 tip에서 P2 branch를 생성했다. 부모가 원본 Editor/PID24376 준비를 확인하고 수명 담당은 provider 공유·재사용의 Edit5/Play7 예정 테스트를 작성한다. 당시 P2 테스트 실행·통과 결과는 없었다. 기존6개 사용자 변경과 main8ce768d는 보존한다.

P2 [실제 Red](validation/ui-system/p2/README.md)는 같은 원본 Editor에서 Edit5/Play7 실패12·skip0을 확인했다. source368개 및 보호6개 unchanged를 기록하고 수명 담당에게 Runtime의 key 공유/재사용 Green을 배정했다. 당시 P2 자동 검증 완료 상태는 아니었다.

P2 sourcecea4268b82e0f119e0d4dbc8c788b7632d21f3ee: 같은 원본 Editor의 [UI 전체30개](validation/ui-system/p2/README.md) Edit14/14+Play16/16 실패0/skip0, compile/제품 Console 오류0, 보호6 unchanged. [회고06](retrospectives/2026-10-08-06-ui-loading-reuse.md)와 source/XML·사람/AI 문서를 갱신해 track에 통합한다. 결과 없는 transport 시도는 별도 보존했다. 다음 P3 contract를 읽기 검토 중이며 Runtime 미구현, P5 읽기 조사만 완료다.

P2 문서/증거15e5eb65edea47ff4e830be54a4e90ea081df14e를 track에 fast-forward 통합하고 같은 tip에서 P3 branch를 생성했다. P2 정적 검사는 문서12개/상대 링크289개, source368개와 보호6개 unchanged 및 source6개 commit 일치를 확인했다. 임시 문서2개는 소유 경로 확인 후 제거했다. P3의 실제 Red 준비와 P5의 Temp 초안은 분리하며 P5 초안은 테스트 실행이나 구현 완료로 기록하지 않는다.

P5 독립 준비: Temp/UI-P5-Draft-20261008의 signature2개/tests2개 초안은 아직 Assets에 반영하지 않았고 실행0건이다. 14개 예정 사례에 규모별 실제 viewport coverage/index-position-Text 일치, native drag/inertia, count/resize/lifetime, binding generation·실제 지연 결과, 고정 viewport warm 후 누적 생성 안정 조건을 포함한다. P5 branch에서 source 반영/컴파일 후 실제 Red를 실행하며 재개에 필요한 Temp4개는 그 전까지 보존한다.

P3 실제 Red: 원본 Editor PID24376 compile/제품 Console 오류0 뒤 Edit4실패4(CLI30396), Play11실패11(CLI4156의 동일run connector-file 결과), skip0. [근거](validation/ui-system/p3/README.md)와 입력372개/보호6개를 별도 보존했다. Runtime 수명 담당에게 Green 구현을 배정하고 테스트 소스는 동결한다. native child Canvas 비활성화 시 상위 Canvas fallback은 별도 관찰로 기록했으며 UIContext 통과 결과가 아니다.

P3 source18666acae25b04c1c63ca49d8c00ca2ee67da308: [최종 UI48](validation/ui-system/p3/README.md) Edit18/18+Play30/30 실패0/skip0, compile/제품 Console0, source375·보호6 unchanged. HUD/parent/host/Canvas/renderer-only 보관을 구현했다. native fixture3건과 Edit 렌더프레임 의존 Timeout2건은 원본 실패 및 당시 입력을 보존하고 테스트 관측만 보정했다. 재사용 Edit 결과file 도구/self-check·XML/사람/AI 문서·[회고07](retrospectives/2026-10-08-07-ui-presentation.md)를 갱신해 track에 통합한다. 다음 P4는 실제 Red17 예정, 아직 실행0건이며 P5/P6/P7 Temp 초안도 미구현이다. main8ce768d·upm/버전/태그/Release는 보존한다.

P3 문서/증거3725ad6를 track에 fast-forward 통합하고 같은 tip에서 codex/game-ui-p4-input을 생성했다. 명시 borrowed EventSystem(생성자 네 번째 optional), native wrapper input gate·독립 modal lease·DisplayChanged 일반 오류와 native fault 구분·user veto·InputSystem adapter의 raw 해제+frame 경계를 [계약](GAME_UI_SYSTEM.md)에 반영한다. 실제P4 Red17(Edit6/corePlay6/InputPlay5) 준비 중이며 실행/통과 결과는 아직 없다. 부모만 Editor/Git를 맡고 P5/P6 독립 Temp 준비는 미실행이다.

P4 실제 Red: 원본 Editor PID24376에서 Edit6/corePlay6/InputPlay5 모두 실패17·skip0을 확인했다. [근거](validation/ui-system/p4/README.md), 입력394개/보호6개 unchanged. 기존 script 등록 누락은 실제 compiler 입력/로드 어셈블리 확인 후 일회 복구했으며 영구 native registry 복구로 주장하지 않는다. UI Runtime/optional adapter의 Green을 수명 담당에게 배정했다. 기존 Core/Input·사용자6파일·main을 보존한다.

P4 독립 구현 배정(2026-10-08): /root/ui_native_input_adapter를 gpt-6.1-sol/high·필요 문맥만 전달하여 생성했다. 실제 Red17 이후 optional UIInputSystemAdapter.cs 한 파일의 native module/held input/독립 lease 복구를 담당한다. 기존 수명 담당은 UIContext partial bridge/handle/wrapper와 테스트만 수정하며 같은 파일을 병렬 수정하지 않는다. 부모가 계약·리뷰·Unity·Git를 유지한다. ui_dependency_audit는 P4 문서/P6 설정·예제/P7 측정의 Temp 초안 준비를 완료하고 현재 비활성이다. 동시 추가 하위 agent는2개 이하이며 재위임은 하지 않는다.

P4 sourcedeb222cf6fda752d3e0dd6d22bda67f9d60e9e16: [최종UI65](validation/ui-system/p4/README.md) Edit24/24+Play41/41 실패0skip0, source398개 고정(보호Sample제외397개 Git일치)/보호6 raw unchanged/원본ready Console0. UI-only modal·explicitEventSystem/optionalInput adapter·Closing/raw-release+frame 차단·focus·forced/userclose·ordinary/nativefault를 구현했다. XML/사람/AI 문서·[회고08](retrospectives/2026-10-08-08-ui-input.md)과 track통합. 원본registry영구복구·소비/Player/Profiler/최종UX는 주장하지 않는다.

P4 최종 문서/회고/정적검사 완료. native 입력 담당과 수명 담당의 P4 source는동결했고 문서담당P4갱신도완료했다. track통합은검증된tip과의ancestor검사/CAS update-ref로수행해이전phase의source삭제/recreate checkout을피한다. 같은tip에서 P5 branch를생성하며 main은8ce768d를유지한다.
