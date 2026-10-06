# 2026-10-06 · BootstrapSystem 최초 진입과 사전 검증

- 목표: 등록 root와 설정을 runtime 이전에 검증하고, 준비된 root로 첫 게임 씬을 Additive 진입한다.
- 기준: main/ee8c0bf9b4fbff51845701b7a8f0647ab5ff63dc, codex/bootstrap-system. 최초 dirty 사용자 자산 3개와 미추적 SceneTemplateSettings는 [hash](../validation/bootstrap-system/preserved-user-inputs.json)로 보존했다.
- 결정: 기존 root와 SceneRootFlow를 재사용한다. BootstrapSystem은 최초 게임 하나만 소유한다. 일반 전환 manager/DI container/setting.asset/프로젝트 씬 자동 생성은 추가하지 않았다. [공용 계약](../BOOTSTRAP_SYSTEM.md)이 API·소유권·설정 본문을 소유한다.
- 변경: BootstrapSystem/Callbacks, root persistence getter, Inspector scene picker, post-compile/live Editor·Play·실제 BuildPlayerOptions.scene 목록 gate, Edit/Play 테스트와 전용 Hub fixture, native Editor 검증 코드·관련 문서.
- 문제/교훈: 테스트 러너 미저장 씬은 NewScene Additive를 허용하지 않아 소유한 empty saved asset을 열었다. build 목록은 Play 이전에 확정되므로 Test Framework PrebuildSetup/PostBuildCleanup을 사용했다. Unity는 누락 씬의 GUID를 다시 계산하므로 원래 serialized Build Settings bytes도 복원한다. 연결 단절은 저장된 fresh 결과로 확인했으며 테스트 실패와 혼동하지 않았다. 늦은 취소는 active scene을 아직 바꾸지 않은 경로에서 불필요한 복원을 요청하지 않도록 수정했다.
- 검증: 실제 Red 설정 19건(16 fail), 진입 6건(6 fail), 중복 Singleton 22건(1 fail), 종료 가림막 1건(1 fail). 최종 Edit 176/176·Play 96/96, fail/skip 0. Bootstrap 전용 22/9 포함. native invalid-root build/Play 차단·fixture 제거, 컴파일 성공·새 Console 오류/경고 0. [증거와 재실행](../validation/bootstrap-system/README.md).
- Console 보완: 최종 EditMode 실행 뒤 기존 SceneRootEditorTests의 AddComponent/Undo 경고 6건을 보존했다. 해당 테스트/코어 메뉴는 수정하지 않았고, 로그 정리 후 새 Console 오류/경고 0을 별도로 확인했다.
- 한계: pre-build 실패의 BuildReport는 Unknown; 실제 BuildFailedException으로 차단을 확인했다. 성공 Player·소비 프로젝트·최종 시각 UX·Reload 비활성 반복 Play는 미실행이다.
- Git: 작업 시작 전 main에서 별도 브랜치 필요를 확인하고 codex/bootstrap-system을 생성했다. 이번 파일만 commit/push하고 필수 자동 검사 통과 후 승인된 조건에 따라 main 통합을 진행한다. 실제 commit/원격 일치는 최종 보고와 Git 이력에서 확인한다.
- 다음: 일반 GameSceneManager의 게임 씬 교체 API·현재 씬 소유권·부분 실패/취소 경계를 정하고 구현한다. Bootstrap 최초 진입 코드와 이번 native 검증을 선행 산출물로 사용한다. 배포 완료 전 소비 프로젝트·Player 검증을 별도 수행한다.

2026-10-06 통합 후 보완: e5ea2be main 병합·푸시와 원격 일치를 확인했다. checkout 시 eol=lf가 적용되어 실행 당시 CRLF/mixed 소스의 raw hash와 달라졌다. 실행 raw hash를 보존하고 검증 커밋의 Git blob과 일치하는 LF 정규화 hash를 추가했다. Runtime/테스트 내용은 변경하지 않았고 정적 대조만 재실행한다.
