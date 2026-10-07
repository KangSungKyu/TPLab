# Scene loading presentation track

2026-10-07. 기준 main `32e6cac1e80c95ec38033f95310f7ac2459e095f`. `codex/scene-loading-track`에서 아래 phase를 순서대로 fast-forward 통합했다. SourceRevision `18d479bf07fe8479a22187ec7357024e9979d096`; 문서·검증 증거를 포함하는 최종 track tip은 Git 이력에서 확인한다. 2026-10-07 사용자의 로딩 UI PlayMode 확인으로 최종 gate를 완료했다. [최종 통합](validation/scene-loading/integration/README.md)에 따라 main 반영·이번 branch 정리를 진행한다.

계약: [현재 설계](SCENE_LOADING_PRESENTATION_DRAFT.md). 기존 가림막과 Single/Additive 소유권을 보존하며 UI는 프로젝트 callback 소유다. 별도 로딩 Unity 씬을 만들지 않는다.

| 단계 | 브랜치 | source tip | 상태 | 증거 |
|---|---|---|---|---|
| 1 진행률 | codex/scene-loading-p1-progress | 8f3bfde | 완료 / main 반영·phase branch 정리 | [P1](validation/scene-loading/p1) |
| 2 표시 순서·3 진행 대기 | codex/scene-loading-p2-flow | 07273a4 | 완료 / main 반영·phase branch 정리 | [P2](validation/scene-loading/p2) |
| 4 예제·통합 검증 | codex/scene-loading-p4-sample | 18d479b | 완료 / 사용자 PlayMode 확인·main 반영·phase branch 정리 | [P4](validation/scene-loading/p4/README.md) |

| 담당 | 모델/추론 | 범위·이유 | 상태 |
|---|---|---|---|
| 부모 | 현재 모델 | 계약·manager·테스트 실행·문서·Git | 자동 검증/최종 통합 담당 |
| /root/input_validation_plan | gpt-6-luna/low | 확정 계약 사람/AI 문서·사용자 체크리스트 | 완료 |
| /root/input_layers | gpt-6.1-sol/high | Resource progress 및 sample; 비동기 결과 소유권 위험 | 구현·읽기 전용 최종 리뷰 완료 |

사용자 변경4와 보호 raw7은 [hash](validation/scene-loading/p4/preserved-inputs.json)로 대조한다. 원본 Editor PID23120만 TPLab 테스트·빌드를 실행했다. build가 생성한 settings 변화만 복원했다. 자동 검사와 실제 최종 UX 확인은 별도 gate다. PC 종료/절전은 이번 요청에 포함되지 않는다.

P1 Red Edit13(2pass/11fail), Play209(201pass/8fail). Green Edit13/13, Play210/210, skip0. short filter0은 통과가 아니다. 기존 condition context와 충돌한 표시context는 SceneLoadingContext로 분리했다.

P2/3는 UI lifetime/대기/old release 순서가 하나의 흐름이므로 한 phase로 통합했다. Red Play21/28(7fail), 최종 targeted36/36. 기존 Core252/224 결과는 이후 edge 보정 전이며 P4 전체 결과로 최종 회귀를 확인한다.

P4 Red UI8/8fail, fixture/module 원인 수정 후11/11. Ready 표시 Red1 재현 후 full Play253/253·final Edit271/271·reload UI12/12, failed0/skip0. Windows Mono 두 빌드 errors0/warnings0, 실제 Additive/Single Player 각12/12. Input포함 별도 소비 Editor build/Player 실제2run 성공. 모든 증거·범위·CLI disconnect 처리·미검증 영역은 [P4 기록](validation/scene-loading/p4/README.md)에 있다.

[최종 사용자 확인](SCENE_LOADING_ACCEPTANCE.md)은 2026-10-07 사용자 PlayMode 확인으로 완료됐다. 자동 Player/synthetic native UI 결과를 화면/물리장치 UX 승인으로 확대하지 않는다. 사용자 확인 후 원격 최신 main을 대조하고 필요하면 track에 통합해 영향을 받는 검증을 실행한다. main push와 원본tip 도달 가능성을 확인하기 전 phase/track 브랜치를 삭제하지 않는다.

## 최종 통합 기록

2026-10-07 사용자 PlayMode 확인을 반영한 `930ddeccc35d3fe5ecb8e4c81b91a20f70fca356`을 main/origin에 fast-forward 반영하고 일치를 확인했다. phase3개는 각 서버 tip SHA를 재확인한 뒤 조건부 삭제하고 로컬 `-d`로 정리했다. [실제 phase 삭제 결과](validation/scene-loading/integration/phase-cleanup.json)에 원본tip·main 보존·로컬/원격 제거를 기록한다. 이 후속 마감 문서도 동일 track에서 main에 fast-forward 반영한 뒤 track만 최종 정리한다. 최종 main/track 삭제tip SHA는 마지막 통합 commit과 최종 보고에서 확인한다. 실행 source는 동일317개이며 사용자 dirty4·보호 raw7을 보존한다.
