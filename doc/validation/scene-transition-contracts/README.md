# SceneTransition Phase 1 검증

2026-10-06. 기준 main/0610ff596f11c23ec0dd96741728b349337ec605, 작업 codex/scene-transition-contracts. MyLab Unity 6000.3.18f1 / Connector 0.4.1 / 기존 Editor PID 23176. [계약과 단계](../../GAME_SCENE_MANAGER_DRAFT.md), [현재 Bootstrap](../../BOOTSTRAP_SYSTEM.md).

| 실행 | total / pass / fail / skip | 증거 |
|---|---|---|
| 공유 callback 주입 Red | 1 / 0 / 1 / 0 | [red-play.json](red-play.json) |
| Bootstrap 전체 Green | 10 / 10 / 0 / 0 | [green-play.json](green-play.json) |
| 최종 전체 EditMode | 178 / 178 / 0 / 0 | [full-EditMode.json](full-EditMode.json) |
| 최종 전체 PlayMode | 97 / 97 / 0 / 0 | [full-PlayMode.json](full-PlayMode.json) |

Red는 새 SceneTransitionCallbacks를 기존 Configure에 runtime 전달할 때 ArgumentException으로 실패했다. 테스트 컴파일 실패나 fixture 준비 실패를 Red로 세지 않았다. 최소 구현 뒤 test 호출을 typed Configure로 정리하고 새 callback에서 설치 후/준비 전 연결, 준비 root 전달, presentation gate 완료 전 reveal/성공 scene 공개 금지, 최종 종료와 공유 root 보존을 확인했다. 기존 9개 진입 테스트는 legacy BootstrapCallbacks override를 계속 사용해 호환 dispatch/실패/취소 회귀를 확인한다. 저장한 씬을 다시 열어 기존/새 callback 객체 참조와 구성 유효성을 확인하는 EditMode 2건도 포함한다.

[실행 기록](execution.json)에 Red/Green 소스 차이와 scope를 기록한다. CLI가 polled 결과를 소비한 뒤 원본을 제거하므로 Red JSON은 실제 CLI stderr 결과를 정규화한 기록이다. 최종 결과는 이번 CLI 출력에서 파싱했으며 과거 bootstrap 결과를 재사용하지 않았다. [입력 hash](test-inputs.json)는 최종 C#/assembly/fixture/의존성 입력을 LF 정규화로 기록한다. [보호 입력](preserved-inputs.json)은 사용자 변경 4개와 EditorBuildSettings의 raw bytes를 검사한다. 기존 callback/probe meta GUID는 변경하지 않았다.

최종 PlayMode의 [예상 테스트 Console](expected-test-console.json)은 LogAssert와 함께 확인한 테스트 실패 경로 로그다. 보존 후 Console을 비우고 [새 Console](final-console.json)과 [Editor ready](final-status.txt)를 별도로 확인했다. 테스트 통과를 전체 실행 중 로그 없음으로 표현하지 않는다.

재사용 [정적 검증 도구](../../../tools/README.md):

```powershell
unity-cli --project C:\Users\PC\Projects\MyLab test --mode EditMode
unity-cli --project C:\Users\PC\Projects\MyLab test --mode PlayMode
python tools/verify_validation.py --evidence doc/validation/scene-transition-contracts
```

verify_validation.py의 실제 최소 실행은 source/result/hash/GUID/link/fixture 정리를 확인한다. check_github_ci.py는 [baseline 원격 검사](ci-baseline.json)에 실제 실행했고 통합 전후에도 정확한 pushed commit으로 검사한다. workflow/check/status/run 0은 CI 미구성이며 CI 통과가 아니다. 필수 검사가 발견되면 통합을 중단하고 해당 정책을 검토한다.

Phase 1은 callback과 문서만 구현한다. 일반 GameSceneManager, Single/영속 Bootstrap, graph·조건·구역 API, 기본 UI와 실제 UX, Player/소비 프로젝트, Reload 비활성 반복 Play는 후속/미실행이다. 이번 연출 자산 변경은 없으며 사용자 직접 확인을 기다릴 새 제품 UX 항목은 없다. [회고](../../retrospectives/2026-10-06-13-scene-transition-contracts.md)에 다음 단계와 한계를 남긴다. 이번 작업의 Temp 스크립트/CLI transcript는 증거 보존·검사 후 제거한다.
