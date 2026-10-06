# 2026-10-06 · 명시적 Build/Addressables 씬 로더

- 목표/기준: track acb636c933f2eea29f46705d233f4e0dba436965에서 codex/game-scenes-p0-loaders로 분기했다. Inspector SceneAsset에서 명시한 source/key와 실제 로드 결과 소유권을 기존 최초 진입에 연결한다.
- 변경/결정: [SceneTarget·로더 계약](../SCENE_LOADING.md). ResourceManager 자산 cache와 씬 instance 수명을 분리하고 GameSceneManager가 결과를 먼저 소유한 뒤 실제 path를 검증한다. 등록 상태로 loader를 자동 선택하거나 fallback하지 않는다. 기존 API·직렬화 field/GUID는 보존했다.
- 배정/리뷰: scene_runtime(6.1-sol/high)은 runtime/테스트, track_guidelines(luna/medium)는 Editor/관련 테스트를 제한 경로에서 작성했다. 부모만 Unity·Git을 담당했고 소스 동결 후 실제 테스트와 최종 ownership 리뷰를 수행했다.
- 문제/해결: UniTask.Addressables asmdef 참조 누락으로 잘못된 await 확장 메서드가 선택되는 컴파일 오류를 수정했다. 빈 inline AssetReference 직렬화와 유효 Build 목록을 요구하는 기존 validation을 분리했다. 전역 Addressables locator 격리 및 IAssetBundleResource fixture 계약을 바로잡았다. SceneProvider의 완료 통지 뒤 backend release 순서 때문에 unload 완료 공개를 callback 반환 뒤로 옮겼다.
- 검증: 실제 Red 16/1/15/0, Editor mapping Red 8/3/5/0. Green runtime 18/18, Editor 13/13, native/Addressables Play 13/13. 최종 전체 EditMode 213/213, PlayMode 128/128, fail/skip 0. [전체 증거와 실패 iteration](../validation/scene-loaders/README.md). compile 완료, 실제 Build/Play invalid Single 거부, 예상 로그 보존 후 Console 오류·경고 0, ready 확인. 사용자 dirty 4개 및 build settings raw bytes 유지.
- 한계: 내부 실패 scene handle reference count 독립 측정·성공 Player·소비 프로젝트·시각/input UX는 미검증이다. 실제 SceneProvider 테스트를 Player 증거로 확대하지 않는다.
- Git/다음: 이번 단위만 commit/push하고 원본 commit을 보존해 track에 통합한다. main 병합·브랜치 삭제는 최종 사용자 확인까지 보류한다. 다음 Phase 3A는 후보 준비 후 주 씬 교체와 Single 이전 graceful shutdown을 TDD로 진행한다. 진행 중 Temp/GameScenesTrack는 공통 보호 hash와 후속 실행 출력에 재사용하고 최종 증거 보존 뒤 명시 목록으로 정리한다.

2026-10-06 통합 보완: 26f725ddf77c2a27e632f48d1f19b7b678315d13을 track에 FF 통합·push하고 main e9fa4e46 유지 및 보호 bytes를 확인했다. [정확한 원격 CI 조회](../validation/scene-loaders/ci-policy.json)는 CI 미구성이다. 보존한 evidence와 LF 정규화 내용 일치를 확인한 P0 임시 JSON 10개만 제거했다. 공통 보호 baseline 및 진행 중 Phase3A 출력은 유지한다.
