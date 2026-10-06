# Phase 3A 주 씬 교체 검증

2026-10-06. 기준 track 26f725ddf77c2a27e632f48d1f19b7b678315d13, 작업 codex/game-scenes-p3a-flow. 기존 MyLab Unity 6000.3.18f1 / Connector 0.4.1 / PID 23176에서 실행했다. [계약](../../GAME_SCENE_MANAGER_DRAFT.md#phase-3a-확정-api와-선행-조건), [회고](../../retrospectives/2026-10-06-18-scene-replacement.md)를 따른다.

| 실제 실행 | total / pass / fail / skip | 증거 |
|---|---|---|
| 컴파일 가능한 API stub Red | 11 / 0 / 11 / 0 | [red-play.json](red-play.json) |
| 주 씬 교체 Green | 18 / 18 / 0 / 0 | [green-play.json](green-play.json) |
| 최종 전체 EditMode | 213 / 213 / 0 / 0 | [full-EditMode.json](full-EditMode.json) |
| 최종 전체 PlayMode | 146 / 146 / 0 / 0 | [full-PlayMode.json](full-PlayMode.json) |

Additive 후보 root/화면 준비 뒤 이전 씬 해제, 후보 실패 시 준비된 이전 root 유지, Single 이전 graceful 종료 대기와 실제 native Single, 반복 교체와 역사적 entry/현재 transition 완료 분리, caller/owner 취소, 늦은 결과의 terminal shutdown 공유 정리를 확인했다. 이전 Release/Unload 실패, reveal 실패와 cover 복원, 이동된 후보와 외부 scene 목록 변경, 마지막 Single 씬의 잔여 진단도 검사했다. 실패 unload 결과를 성공으로 바꾸거나 자동 재시도하지 않는다.

18건에는 실제 Red 11건과 Green 보완 때 추가한 경계 7건이 포함된다. 추가 7건 전체의 사전 Red 실행 증거는 없다. 완료 통지가 대기 코드를 동기 재개하는 경우 다음 작업을 즉시 시작·취소해도 이전 작업이 새 CTS를 해제하지 않는 것을 검사했다. 핵심 flow는 기존 SceneRootFlow, root의 공유 terminal Shutdown, LoadedScene 결과를 재사용했다. 공용 root 준비는 공유하고 게임 교체에서 해제하지 않는다.

두 번째 공식 fixture ReplacementMain은 새 GUID를 사용한다. 테스트 build list는 기존 snapshot/raw bytes 복원 절차를 재사용했다. [보존 hash](preserved-inputs.json)의 사용자 변경 4개와 EditorBuildSettings는 시작 상태와 같다. [최종 입력](test-inputs.json)은 검증한 코드·assembly·fixture·의존성·도구를 기록한다. compile 완료 및 [ready](final-status.txt), [예상 테스트 로그](expected-test-console.json) 보존·clear 후 [Console 오류/경고 0](final-console.json)을 확인했다. domain reload CLI 단절의 focused 결과는 그 CLI PID의 fresh Connector 결과이며, 최종 전체 실행은 정상 CLI 결과다.

```powershell
python tools/run_unity_tests.py --project C:/Users/PC/Projects/MyLab --mode PlayMode --filter MyLab.Core.Tests.GameSceneReplacementTests --output Temp/MyCheck/replace.json
python tools/verify_validation.py --evidence doc/validation/scene-replacement
```

정적 verifier는 Unity 실행을 대신하지 않는다. 이번 단위는 코드 API이며 구역 tree·정의/조건·Inspector 전환 설정·성공 Player·소비 프로젝트·실제 UI는 후속 Phase다. 실제 native Single/시스템 callback 테스트를 시각 UX 확인으로 확대하지 않는다. main 통합과 브랜치 삭제는 최종 명시적 사용자 확인까지 보류한다.
