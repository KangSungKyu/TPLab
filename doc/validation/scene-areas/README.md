# Phase 3B 파생 구역 검증

2026-10-06. 기준 track bd7698ab0d341dd990efb894804d19963a638693, 작업 codex/game-scenes-p3b-areas. 기존 MyLab Unity 6000.3.18f1 / Connector 0.4.1 / PID 23176에서 실행했다. [계약](../../GAME_SCENE_MANAGER_DRAFT.md#phase-3b-구현-api와-실행-경계), [회고](../../retrospectives/2026-10-06-19-scene-areas.md)를 따른다.

| 실제 실행 | total / pass / fail / skip | 증거 |
|---|---|---|
| 컴파일 가능한 API stub Red | 18 / 0 / 18 / 0 | [red-play.json](red-play.json) |
| 최초 파생 구역 Green | 26 / 26 / 0 / 0 | [initial-green-play.json](initial-green-play.json) |
| unload 이후 reveal 대기 공유 Red | 1 / 0 / 1 / 0 | [reveal-red-play.json](reveal-red-play.json) |
| 최종 파생 구역 Green | 27 / 27 / 0 / 0 | [green-play.json](green-play.json) |
| 최종 전체 EditMode | 213 / 213 / 0 / 0 | [full-EditMode.json](full-EditMode.json) |
| 최종 전체 PlayMode | 173 / 173 / 0 / 0 | [full-PlayMode.json](full-PlayMode.json) |

부모/자식의 실제 Scene 등록, priority snapshot, active 유지/명시 선택, 자식 자기 종료와 ancestor 권한, 같은 진행 중 제거의 공유 완료를 검사했다. primary 교체·구역 제거·terminal Shutdown은 자식부터 정리한다. 늦은 추가 후보의 소유권, 시작한 subtree 정리 중 owner 취소, root release/Unload 실패, 외부 active/inventory 변경 및 종료 후 잔여 소유권도 확인했다.

27건은 실제 Red 18건, Green 보완 경계 8건, 리뷰 후 추가 Red 1건이다. 추가 8건의 별도 사전 Red는 미실행이다. 대상 unload 뒤 비동기 reveal 동안에도 보존된 제거 작업 record로 self/생존 ancestor 권한을 확인하여 같은 완료를 공유한다. sibling/default requester는 거부한다. Add 결과는 작업별 실제 Scene을 저장하여 다음 동기 awaiter의 명령과 분리한다. 제거되는 active subtree는 가장 가까운 생존 부모를 선택하며, subtree 밖 active sibling은 유지한다. 실패한 unload는 성공으로 바꾸거나 자동 재시도하지 않는다.

[최종 입력](test-inputs.json) 199개와 [보존 파일](preserved-inputs.json) 5개의 raw bytes를 확인했다. 컴파일 완료, [Editor ready](final-status.txt), [예상 테스트 로그](expected-test-console.json) 10건 보존 후 [Console 오류·경고 0](final-console.json)을 확인했다. 최종 전체 Play는 해당 CLI PID의 fresh Connector 결과로 보존했다. 테스트 build scene 목록은 기존 setup의 raw snapshot/복원을 재사용한다.

최초 전체 Edit 시도는 CLI discovery에서 실행 전에 거부되어 결과가 없었으며 통과로 세지 않았다. [도구 검사](driver-checks.json)는 그 실패와 self-check Red/Green을 기록한다. runner는 정확한 실행 전 discovery 오류에만 동일 Editor PID를 다시 확인하고 한 번 더 접수한다. 실제 테스트 실패·모호한 결과를 재실행하지 않는다. 이후 실제 전체 Edit 213건이 통과했다.

정의/조건·Inspector·성공 Player·소비 프로젝트·실제 UI는 후속 Phase다. main 통합과 브랜치 삭제는 최종 명시적 사용자 확인까지 보류한다.

```powershell
python tools/run_unity_tests.py --project C:/Users/PC/Projects/MyLab --mode PlayMode --filter MyLab.Core.Tests.GameSceneAreaTests --output Temp/MyCheck/areas.json
python tools/verify_validation.py --evidence doc/validation/scene-areas
```
