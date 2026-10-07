# TPLab 이름 변경 검증

2026-10-07. 기준 main `11d62236edfac6d3c85f02daa5a3da7ad7d98d8f`, 작업 브랜치 `codex/tplab-naming`. 이름 변경 소스 `6d0139efe3a217d33b6c3e2f811e03476895e4da`, 파일 복원 수정 포함 소스 `3062716f2d494bc61bf515f3fa30b1ee8aada9f0`.

## 범위

`Assets/MyLab`을 `Assets/TPLab`으로 이동하고 namespace·assembly·메뉴·importer 생성 문자열·예제 SceneTarget·Player 인수·소비 검증 도구·현재 사람/AI 문서를 통일한다. Runtime API의 동작은 유지한다. UPM 구조 전환·배포물 생성·버전 태그 발행은 포함하지 않는다. [변경 안내](../../TPLAB_NAMING.md)를 따른다.

## 실제 실행

- [최초 EditMode](red-edit.json): 271개 중 pass269/fail2/skip0. SceneTransitionEditorTests TearDown의 File.WriteAllBytes에서 Win32 IO 1224가 발생했다. 실패를 성공으로 대체하지 않는다.
- Unity가 내부 캐시한 파일 핸들을 해제하도록 테스트·예제의 Build Settings 복원 직전에 AssetDatabase.ReleaseCachedFileHandles를 추가했다. 테스트는 복원 bytes 일치도 확인한다. [Unity 공식 API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.ReleaseCachedFileHandles.html)는 자산 수정 시 파일 공유 오류를 방지하기 위한 API다.
- [수정 후 EditMode](full-edit.json): pass271/fail0/skip0. 원래 Editor PID23120, Unity6000.3.18f1, Connector0.4.1.
- [PlayMode](full-play.json): pass253/fail0/skip0. 동일 원래 Editor에서 실행했다.
- 실제 Windows Mono 예제 빌드: [Additive](../scene-loading/tplab-naming-build-additive.json)·[Single](../scene-loading/tplab-naming-build-single.json), 각각 errors0/warnings0. 씬 setup·backend·Build Settings 복원 성공.
- 실제 Player: [Additive](../scene-loading/tplab-naming-player-additive/scene-player-additive-20261007T064834Z-31168.json)·[Single](../scene-loading/tplab-naming-player-single/scene-player-single-20261007T064926Z-26268.json), 각각 completedChecks12/12·exit0. 새 `-tplab-*` 인수로 native 전환·로딩·해제 smoke를 확인했다.
- 독립 최소 소비 프로젝트: [Core만](../scene-loading/tplab-naming-consumer-core/core-consumer-20261007T064913Z-19632.json)·[Core+Input](../scene-loading/tplab-naming-consumer-input/core-consumer-20261007T064538Z-36104.json), 각각 Editor build1회와 Player1회 성공(actualRuns2). 게임별 코드 없이 동일 Unity6000.3.18f1·Mono에서 가져오기·실행을 확인한다. 소비 로그는 각 폴더의 preserved-logs.json이 영속 경로를 기록한다.
- [Native 씬 검사](native-scenes.json): 사용자 InitScene과 예제6개 각각 root1/누락스크립트0, loaded old MyLab assembly0, productName TPLab.
- [정적 이름/GUID 검사](rename-static.json): metadata180개 raw bytes 유지, GUID180개 중복0, assembly11개 새 이름, 문서104개 링크 확인. [source 입력](test-inputs.json)317개와 [보호 입력](preserved-inputs.json)7개 검증. [빌드 자동 변경 복원](build-settings-restoration.json)은 명시한 설정7개만 원복하고 새 프로젝트 이름을 유지한다.
- run_core_consumer·run_scene_player·capture_validation_inputs self-check 각각1회 통과. 최초 Player 인수의 output 경로가 allowlist 밖이라 실행 전 거부1회였으며 실제 Player 실행0건이었다. 올바른 Temp/GameScenesTrack 경로의 새 실행을 사용한다.
- [최종 Console](final-console.json)은 오류·경고0, [원래 Editor](final-status.txt) ready/PID23120. [소스 GitHub 검사](source-ci.json)는 main 비보호·workflow/check/status/run0으로 CI 미구성을 확인했으며 CI 성공이라고 표현하지 않는다.

## 보호와 한계

`.meta` raw bytes와 GUID는 그대로 유지한다. 사용자 InitScene 오브젝트와 값은 보존하고 식별 문자열만 `MyLab.Core`에서 `TPLab.Core`로 바꾼다. 이 사용자 씬은 작업 커밋에 포함하지 않는다. 다른 기존 dirty3과 보호된 InputAction/Build Settings는 원본 bytes로 확인한다. ProjectSettings의 이름 네 필드만 이번 변경이다.

물리 checkout 경로는 `C:\Users\PC\Projects\MyLab`이며 같은 기존 Editor를 사용한다. 필요한 자동 검증과 코드/문서 diff 리뷰를 완료했으며 새 화면·동작 변경이 없어 추가 시각 확인 항목은 없다. 기존 승인에 따라 최신 원격·최종 커밋 검사 뒤 main fast-forward 통합과 작업 브랜치 정리를 진행한다. 다른 Unity·플랫폼·IL2CPP·실제 외부 PC와 배포 패키지 설치 검증은 미실행이다.
