# 씬 로딩 진행률 P1

2026-10-07. 기준 main32e6cac, codex/scene-loading-p1-progress. Backend stage/immutable snapshot을 구현했다. 기존 ISceneLoader 호환·선택적 overload·동기 observer를 제공하며 UI 예외는 native 결과 확보 후 후보 정리로 전달한다. 설치된 UniTask의 bytes 비율 대신 PercentComplete를 읽고 native1과 root 준비를 구분한다.

Red Edit13 중11fail·Play209 중 신규8fail, Green Edit13/13·core Play210/210, failed0/skip0. [증거](../validation/scene-loading/p1/). 컴파일 오류0. short 필터0건은 통과가 아니고 표시context 이름 충돌은 SceneLoadingContext로 수정했다. 보호7입력 대조·build GUID bytes 복원. Player/consumer/최종UX는 후속phase. P1 commit/track통합 후 opt-in 두cover/대기/sample 작업을 진행하며 main은 최종확인대기다. Temp/SceneLoading/protected는 최종 보호 대조 후 제거한다.
