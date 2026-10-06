# Phase 4 전환 정의와 root 조건 검증

2026-10-06. 기준 track `54107893f9da91e86f44be5766c5501e8de8e0df`, 작업 `codex/game-scenes-p4-definitions`. 기존 MyLab Unity 6000.3.18f1 / Connector 0.4.1 / PID 23176에서 실행했다. [계약](../../GAME_SCENE_MANAGER_DRAFT.md), [회고](../../retrospectives/2026-10-06-20-scene-definitions.md)를 따른다.

| 실제 실행 | total / pass / fail / skip | 증거 |
|---|---|---|
| 최초 settings Red | 13 / 1 / 12 / 0 | [red-edit.json](red-edit.json) |
| 최초 policy Red | 25 / 0 / 25 / 0 | [red-play.json](red-play.json) |
| Bootstrap effective getter Red | 1 / 0 / 1 / 0 | [getters-red-edit.json](getters-red-edit.json) |
| 비동기 reveal 이후 조건 Red | 1 / 0 / 1 / 0 | [reveal-red-play.json](reveal-red-play.json) |
| 최종 settings Green | 15 / 15 / 0 / 0 | [green-edit.json](green-edit.json) |
| 최종 policy Green | 26 / 26 / 0 / 0 | [green-play.json](green-play.json) |
| 전체 EditMode | 228 / 228 / 0 / 0 | [full-EditMode.json](full-EditMode.json) |
| 전체 PlayMode | 199 / 199 / 0 / 0 | [full-PlayMode.json](full-PlayMode.json) |

정의 snapshot과 입력 목록의 분리, JSON 직렬화의 빈 Build key, 종류/모드/ID 검증, 실제 source instance와 제거 권한을 검사했다. ID·직접 요청·편의 API는 같은 정책 경로를 사용한다. disabled를 포함한 root 조건, required ID, 조건 getter/Evaluate의 재진입, 해제 전 재검사, first/add의 reveal 전후 재검사, 늦은 false의 취소 토큰·cover 복원·후보 정리 및 pending 제거 공유를 확인했다. Bootstrap의 effective getter는 선택 정의를 노출하며 기존 직렬화 값은 보존한다.

최초 Red의 1건은 passive DTO 계약이 이미 구현되어 통과했다. FirstEntry source path 경계 1건과 일부 취소 토큰 assertion의 별도 사전 Red는 미실행이다. reveal Red의 최초 접수는 CLI discovery에서 실행 전에 실패하여 결과가 없었고 통과로 세지 않았다. runner의 project 인자를 기존 status 조회와 같은 절대 forward-slash 형태로 통일한 뒤 실제 Red 1건을 확인했다. 원인이 path 표기였다고 확정하지 않는다. 실제 테스트 실패를 자동 재실행하지 않는다.

[최종 입력](test-inputs.json) 217개와 [보존 파일](preserved-inputs.json) 5개의 raw bytes를 확인했다. 컴파일 완료, [원래 Editor ready](final-status.txt), [기존 실패 경로의 예상 로그](expected-test-console.json) 10건 보존 후 [Console 오류·경고 0](final-console.json)을 확인했다. runtime 읽기 리뷰에서 추가 결함을 찾지 못했다. Inspector/정의 사전 gate·성공 Player·소비 프로젝트·시각/input UX는 후속 Phase이며 main 통합과 브랜치 삭제는 사용자 확인까지 보류한다.

```powershell
python tools/run_unity_tests.py --project C:/Users/PC/Projects/MyLab --mode PlayMode --filter MyLab.Core.Tests.SceneTransitionPolicyTests --output Temp/MyCheck/policy.json
python tools/verify_validation.py --evidence doc/validation/scene-definitions
```
